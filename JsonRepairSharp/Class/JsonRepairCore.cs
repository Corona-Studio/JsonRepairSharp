using System.Collections.Frozen;
using System.Text.Json;
using System.Text.RegularExpressions;
using JsonRepairSharp.Class.Helpers;

namespace JsonRepairSharp.Class;

public partial class JsonRepairCore
{
    private static readonly FrozenDictionary<char, string> ControlCharacters = new Dictionary<char, string>
    {
        { '\b', "\\b" },
        { '\f', "\\f" },
        { '\n', "\\n" },
        { '\r', "\\r" },
        { '\t', "\\t" }
    }.ToFrozenDictionary();

    // map with all escape characters
    private static readonly FrozenDictionary<char, string> EscapeCharacters = new Dictionary<char, string>
    {
        { '"', "\"" },
        { '\\', "\\" },
        { '/', "/" },
        { 'b', "\b" },
        { 'f', "\f" },
        { 'n', "\n" },
        { 'r', "\r" },
        { 't', "\t" }
        // note that \u is handled separately in parseString()
    }.ToFrozenDictionary();

    private int _i;
    private string _output = null!;
    private string _text = null!;

    public string JsonRepair(string text)
    {
        _i = 0; // current index in text
        _output = ""; // generated output
        _text = text; // input text

        var processed = ParseValue();
        if (!processed) ThrowUnexpectedEnd();

        var processedComma = ParseCharacter(StringHelper.CodeComma);
        if (processedComma) ParseWhitespaceAndSkipComments();

        if (_i < _text.Length && StringHelper.IsStartOfValue(text[_i].ToString()) && StringHelper.EndsWithCommaOrNewline(_output))
        {
            // start of a new value after end of the root level object: looks like
            // newline delimited JSON -> turn into a root level array
            if (!processedComma)
                // repair missing comma
                _output = StringHelper.InsertBeforeLastWhitespace(_output, ",");

            ParseNewlineDelimitedJson();
        }
        else if (processedComma)
        {
            // repair: remove trailing comma
            _output = StringHelper.StripLastOccurrence(_output, ",");
        }

        // repair redundant end quotes
        while (_i < text.Length &&
               (text[_i] == StringHelper.CodeClosingBrace || text[_i] == StringHelper.CodeClosingBracket))
        {
            _i++;
            ParseWhitespaceAndSkipComments();
        }

        if (_i >= text.Length)
            // reached the end of the document properly
            return _output;

        ThrowUnexpectedCharacter();
        return
            _output; // This line is added to satisfy C# compiler, as it doesn't recognize that ThrowUnexpectedCharacter always throws
    }

    private bool ParseValue()
    {
        ParseWhitespaceAndSkipComments();
        var processed =
            ParseObject() ||
            ParseArray() ||
            ParseString() ||
            ParseNumber() ||
            ParseKeywords() ||
            ParseUnquotedString(false) ||
            ParseRegex();
        ParseWhitespaceAndSkipComments();

        return processed;
    }

    private bool ParseWhitespaceAndSkipComments(bool skipNewline = true)
    {
        var start = _i;

        // ReSharper disable once RedundantAssignment
        var changed = ParseWhitespace(skipNewline);
        do
        {
            changed = ParseComment();
            if (changed) changed = ParseWhitespace(skipNewline);
        } while (changed);

        return _i > start;
    }

    private bool ParseWhitespace(bool skipNewline)
    {
        Func<int, bool> isWhiteSpace =
            skipNewline ? StringHelper.IsWhitespace : StringHelper.IsWhitespaceExceptNewline;
        var whitespace = "";

        while (true)
        {
            if (_i >= _text.Length) break;
            var c = (int)_text[_i];
            if (isWhiteSpace(c))
            {
                whitespace += _text[_i];
                _i++;
            }
            else if (StringHelper.IsSpecialWhitespace(c))
            {
                // repair special whitespace
                whitespace += " ";
                _i++;
            }
            else
            {
                break;
            }
        }

        if (whitespace.Length > 0)
        {
            _output += whitespace;
            return true;
        }

        return false;
    }

    private bool ParseComment()
    {
        // find a block comment '/* ... */'
        if (_i + 1 < _text.Length && _text[_i] == StringHelper.CodeSlash && _text[_i + 1] == StringHelper.CodeAsterisk)
        {
            // repair block comment by skipping it
            while (_i < _text.Length && !AtEndOfBlockComment(_text, _i)) _i++;

            _i += 2;

            return true;
        }

        // find a line comment '// ...'
        if (_i + 1 < _text.Length && _text[_i] == StringHelper.CodeSlash && _text[_i + 1] == StringHelper.CodeSlash)
        {
            // repair line comment by skipping it
            while (_i < _text.Length && _text[_i] != StringHelper.CodeNewline) _i++;

            return true;
        }

        return false;
    }

    private bool ParseCharacter(char code)
    {
        if (_i < _text.Length && _text[_i] == code)
        {
            _output += _text[_i];
            _i++;
            return true;
        }

        return false;
    }

    private bool SkipCharacter(char code)
    {
        if (_i < _text.Length && _text[_i] == code)
        {
            _i++;
            return true;
        }

        return false;
    }

    private bool SkipEscapeCharacter()
    {
        return SkipCharacter(StringHelper.CodeBackslash);
    }

    /// <summary>
    ///     Skip ellipsis like "[1,2,3,...]" or "[1,2,3,...,9]" or "[...,7,8,9]"
    ///     or a similar construct in objects.
    /// </summary>
    private bool SkipEllipsis()
    {
        ParseWhitespaceAndSkipComments();

        if (_i + 2 < _text.Length &&
            _text[_i] == StringHelper.CodeDot &&
            _text[_i + 1] == StringHelper.CodeDot &&
            _text[_i + 2] == StringHelper.CodeDot)
        {
            // repair: remove the ellipsis (three dots) and optionally a comma
            _i += 3;
            ParseWhitespaceAndSkipComments();
            SkipCharacter(StringHelper.CodeComma);

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Parse an object like '{"key": "value"}'
    /// </summary>
    private bool ParseObject()
    {
        if (_i < _text.Length && _text[_i] == StringHelper.CodeOpeningBrace)
        {
            _output += "{";
            _i++;
            ParseWhitespaceAndSkipComments();

            // repair: skip leading comma like in {, message: "hi"}
            if (SkipCharacter(StringHelper.CodeComma)) ParseWhitespaceAndSkipComments();

            var initial = true;
            while (_i < _text.Length && _text[_i] != StringHelper.CodeClosingBrace)
            {
                bool processedComma;
                if (!initial)
                {
                    processedComma = ParseCharacter(StringHelper.CodeComma);
                    if (!processedComma)
                        // repair missing comma
                        _output = StringHelper.InsertBeforeLastWhitespace(_output, ",");

                    ParseWhitespaceAndSkipComments();
                }
                else
                {
                    processedComma = true;
                    initial = false;
                }

                SkipEllipsis();

                var processedKey = ParseString() || ParseUnquotedString(true);
                if (!processedKey)
                {
                    if (_i < _text.Length && (
                            _text[_i] == StringHelper.CodeClosingBrace ||
                            _text[_i] == StringHelper.CodeOpeningBrace ||
                            _text[_i] == StringHelper.CodeClosingBracket ||
                            _text[_i] == StringHelper.CodeOpeningBracket))
                        // repair trailing comma
                        _output = StringHelper.StripLastOccurrence(_output, ",");
                    else
                        ThrowObjectKeyExpected();

                    break;
                }

                ParseWhitespaceAndSkipComments();
                var processedColon = ParseCharacter(StringHelper.CodeColon);
                var truncatedText = _i >= _text.Length;
                if (!processedColon)
                {
                    if (StringHelper.IsStartOfValue(_text[_i].ToString()) || truncatedText)
                        // repair missing colon
                        _output = StringHelper.InsertBeforeLastWhitespace(_output, ":");
                    else
                        ThrowColonExpected();
                }

                var processedValue = ParseValue();
                if (!processedValue)
                {
                    if (processedColon || truncatedText)
                        // repair missing object value
                        _output += "null";
                    else
                        ThrowColonExpected();
                }
            }

            if (_i < _text.Length && _text[_i] == StringHelper.CodeClosingBrace)
            {
                _output += "}";
                _i++;
            }
            else
            {
                // repair missing end bracket
                _output = StringHelper.InsertBeforeLastWhitespace(_output, "}");
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Parse an array like '["item1", "item2", ...]'
    /// </summary>
    private bool ParseArray()
    {
        if (_i < _text.Length && _text[_i] == StringHelper.CodeOpeningBracket)
        {
            _output += "[";
            _i++;
            ParseWhitespaceAndSkipComments();

            // repair: skip leading comma like in [,1,2,3]
            if (SkipCharacter(StringHelper.CodeComma)) ParseWhitespaceAndSkipComments();

            var initial = true;
            while (_i < _text.Length && _text[_i] != StringHelper.CodeClosingBracket)
            {
                if (!initial)
                {
                    var processedComma = ParseCharacter(StringHelper.CodeComma);
                    if (!processedComma)
                        // repair missing comma
                        _output = StringHelper.InsertBeforeLastWhitespace(_output, ",");
                }
                else
                {
                    initial = false;
                }

                SkipEllipsis();

                var processedValue = ParseValue();
                if (!processedValue)
                {
                    // repair trailing comma
                    _output = StringHelper.StripLastOccurrence(_output, ",");
                    break;
                }
            }

            if (_i < _text.Length && _text[_i] == StringHelper.CodeClosingBracket)
            {
                _output += "]";
                _i++;
            }
            else
            {
                // repair missing closing array bracket
                _output = StringHelper.InsertBeforeLastWhitespace(_output, "]");
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Parse and repair Newline Delimited JSON (NDJSON):
    ///     multiple JSON objects separated by a newline character
    /// </summary>
    private void ParseNewlineDelimitedJson()
    {
        // repair NDJSON
        var initial = true;
        var processedValue = true;
        while (processedValue)
        {
            if (!initial)
            {
                // parse optional comma, insert when missing
                var processedComma = ParseCharacter(StringHelper.CodeComma);
                if (!processedComma)
                    // repair: add missing comma
                    _output = StringHelper.InsertBeforeLastWhitespace(_output, ",");
            }
            else
            {
                initial = false;
            }

            processedValue = ParseValue();
        }

        if (!processedValue)
            // repair: remove trailing comma
            _output = StringHelper.StripLastOccurrence(_output, ",");

        // repair: wrap the output inside array brackets
        _output = $"[\n{_output}\n]";
    }

    /// <summary>
    ///     Parse a string enclosed by double quotes "...". Can contain escaped quotes
    ///     Repair strings enclosed in single quotes or special quotes
    ///     Repair an escaped string
    ///     The function can run in two stages:
    ///     - First, it assumes the string has a valid end quote
    ///     - If it turns out that the string does not have a valid end quote followed
    ///     by a delimiter (which should be the case), the function runs again in a
    ///     more conservative way, stopping the string at the first next delimiter
    ///     and fixing the string by inserting a quote there, or stopping at a
    ///     stop index detected in the first iteration.
    /// </summary>
    private bool ParseString(bool stopAtDelimiter = false, int stopAtIndex = -1)
    {
        var skipEscapeChars = _text[_i] == '\\';
        if (skipEscapeChars)
        {
            // repair: remove the first escape character
            _i++;
            skipEscapeChars = true;
        }

        if (StringHelper.IsQuote(_text[_i]))
        {
            // double quotes are correct JSON,
            // single quotes come from JavaScript for example, we assume it will have a correct single end quote too
            // otherwise, we will match any double-quote-like start with a double-quote-like end,
            // or any single-quote-like start with a single-quote-like end
            Func<int, bool> isEndQuote = StringHelper.IsDoubleQuote(_text[_i])
                ? StringHelper.IsDoubleQuote
                : StringHelper.IsSingleQuote(_text[_i])
                    ? StringHelper.IsSingleQuote
                    : StringHelper.IsSingleQuoteLike(_text[_i])
                        ? StringHelper.IsSingleQuoteLike
                        : StringHelper.IsDoubleQuoteLike;

            var iBefore = _i;
            var oBefore = _output.Length;

            var str = "\"";
            _i++;

            while (true)
            {
                if (_i >= _text.Length)
                {
                    // end of text, we are missing an end quote
                    var iPrev = PrevNonWhitespaceIndex(_i - 1);
                    if (!stopAtDelimiter && StringHelper.IsDelimiter(_text[iPrev].ToString()))
                    {
                        // if the text ends with a delimiter, like ["hello],
                        // so the missing end quote should be inserted before this delimiter
                        // retry parsing the string, stopping at the first next delimiter
                        _i = iBefore;
                        _output = _output.Substring(0, oBefore);

                        return ParseString(true);
                    }

                    // repair missing quote
                    str = StringHelper.InsertBeforeLastWhitespace(str, "\"");
                    _output += str;

                    return true;
                }

                if (_i == stopAtIndex)
                {
                    // use the stop index detected in the first iteration, and repair end quote
                    str = StringHelper.InsertBeforeLastWhitespace(str, "\"");
                    _output += str;

                    return true;
                }

                if (isEndQuote(_text[_i]))
                {
                    // end quote
                    // let us check what is before and after the quote to verify whether this is a legit end quote
                    var iQuote = _i;
                    var oQuote = str.Length;
                    str += "\"";
                    _i++;
                    _output += str;

                    ParseWhitespaceAndSkipComments(false);
                    if (stopAtDelimiter ||
                        _i >= _text.Length ||
                        StringHelper.IsDelimiter(_text[_i].ToString()) ||
                        StringHelper.IsQuote(_text[_i]) ||
                        StringHelper.IsDigit(_text[_i]))
                    {
                        // The quote is followed by the end of the text, a delimiter,
                        // or a next value. So the quote is indeed the end of the string.
                        ParseConcatenatedString();

                        return true;
                    }

                    var iPrevChar = PrevNonWhitespaceIndex(iQuote - 1);
                    var prevChar = _text[iPrevChar];

                    if (prevChar == ',')
                    {
                        // A comma followed by a quote, like '{"a":"b,c,"d":"e"}'.
                        // We assume that the quote is a start quote, and that the end quote
                        // should have been located right before the comma but is missing.
                        _i = iBefore;
                        _output = _output.Substring(0, oBefore);

                        return ParseString(false, iPrevChar);
                    }

                    if (StringHelper.IsDelimiter(prevChar.ToString()))
                    {
                        // This is not the right end quote: it is preceded by a delimiter,
                        // and NOT followed by a delimiter. So, there is an end quote missing
                        // parse the string again and then stop at the first next delimiter
                        _i = iBefore;
                        _output = _output.Substring(0, oBefore);

                        return ParseString(true);
                    }

                    // revert to right after the quote but before any whitespace, and continue parsing the string
                    _output = _output.Substring(0, oBefore);
                    _i = iQuote + 1;

                    // repair unescaped quote
                    str = $"{str.Substring(0, oQuote)}\\{str.Substring(oQuote)}";
                }
                else if (stopAtDelimiter && StringHelper.IsUnquotedStringDelimiter(_text[_i].ToString()))
                {
                    // we're in the mode to stop the string at the first delimiter
                    // because there is an end quote missing

                    // test start of an url like "https://..." (this would be parsed as a comment)
                    if (_text[_i - 1] == ':' &&
                        StringHelper.RegexUrlStart().IsMatch(_text.Substring(iBefore + 1,
                            Math.Min(_i + 2 - (iBefore + 1), _text.Length - (iBefore + 1)))))
                        while (_i < _text.Length && StringHelper.RegexUrlChar().IsMatch(_text[_i].ToString()))
                        {
                            str += _text[_i];
                            _i++;
                        }

                    // repair missing quote
                    str = StringHelper.InsertBeforeLastWhitespace(str, "\"");
                    _output += str;

                    ParseConcatenatedString();

                    return true;
                }
                else if (_text[_i] == '\\')
                {
                    // handle escaped content like \n or \u2605
                    var nextChar = _text[_i + 1];
                    string escapeChar;
                    if (EscapeCharacters.TryGetValue(nextChar, out escapeChar))
                    {
                        str += _text.Substring(_i, 2);
                        _i += 2;
                    }
                    else if (nextChar == 'u')
                    {
                        var j = 2;
                        while (j < 6 && StringHelper.IsHex(_text[_i + j])) j++;

                        if (j == 6)
                        {
                            str += _text.Substring(_i, 6);
                            _i += 6;
                        }
                        else if (_i + j >= _text.Length)
                        {
                            // repair invalid or truncated unicode char at the end of the text
                            // by removing the unicode char and ending the string here
                            _i = _text.Length;
                        }
                        else
                        {
                            ThrowInvalidUnicodeCharacter();
                        }
                    }
                    else
                    {
                        // repair invalid escape character: remove it
                        str += nextChar;
                        _i += 2;
                    }
                }
                else
                {
                    // handle regular characters
                    var currentChar = _text[_i];

                    if (currentChar == '\"' && _text[_i - 1] != '\\')
                    {
                        // repair unescaped double quote
                        str += $"\\{currentChar}";
                        _i++;
                    }
                    else if (StringHelper.IsControlCharacter(currentChar))
                    {
                        // unescaped control character
                        str += ControlCharacters[currentChar];
                        _i++;
                    }
                    else
                    {
                        if (!StringHelper.IsValidStringCharacter(currentChar))
                            ThrowInvalidCharacter(currentChar.ToString());

                        str += currentChar;
                        _i++;
                    }
                }

                if (skipEscapeChars)
                    // repair: skipped escape character (nothing to do)
                    SkipEscapeCharacter();
            }
        }

        return false;
    }

    /**
 * Repair concatenated strings like "hello" + "world", change this into "helloworld"
 */
    private bool ParseConcatenatedString()
    {
        var processed = false;

        ParseWhitespaceAndSkipComments();
        while (_text[_i] == StringHelper.CodePlus)
        {
            processed = true;
            _i++;
            ParseWhitespaceAndSkipComments();

            // repair: remove the end quote of the first string
            _output = StringHelper.StripLastOccurrence(_output, "\"", true);
            var start = _output.Length;
            var parsedStr = ParseString();
            if (parsedStr)
                // repair: remove the start quote of the second string
                _output = StringHelper.RemoveAtIndex(_output, start, 1);
            else
                // repair: remove the + because it is not followed by a string
                _output = StringHelper.InsertBeforeLastWhitespace(_output, "\"");
        }

        return processed;
    }

    /**
     * Parse a number like 2.4 or 2.4e6
     */
    private bool ParseNumber()
    {
        var start = _i;
        if (_text[_i] == StringHelper.CodeMinus)
        {
            _i++;
            if (AtEndOfNumber())
            {
                RepairNumberEndingWithNumericSymbol(start);
                return true;
            }

            if (!StringHelper.IsDigit(_text[_i]))
            {
                _i = start;
                return false;
            }
        }

        // Note that in JSON leading zeros like "00789" are not allowed.
        // We will allow all leading zeros here though and at the end of ParseNumber
        // check against trailing zeros and repair that if needed.
        // Leading zeros can have meaning, so we should not clear them.
        while (StringHelper.IsDigit(_text[_i])) _i++;

        if (_text[_i] == StringHelper.CodeDot)
        {
            _i++;
            if (AtEndOfNumber())
            {
                RepairNumberEndingWithNumericSymbol(start);
                return true;
            }

            if (!StringHelper.IsDigit(_text[_i]))
            {
                _i = start;
                return false;
            }

            while (StringHelper.IsDigit(_text[_i])) _i++;
        }

        if (_text[_i] == StringHelper.CodeLowercaseE || _text[_i] == StringHelper.CodeUppercaseE)
        {
            _i++;
            if (_text[_i] == StringHelper.CodeMinus || _text[_i] == StringHelper.CodePlus) _i++;

            if (AtEndOfNumber())
            {
                RepairNumberEndingWithNumericSymbol(start);
                return true;
            }

            if (!StringHelper.IsDigit(_text[_i]))
            {
                _i = start;
                return false;
            }

            while (StringHelper.IsDigit(_text[_i])) _i++;
        }

        // if we're not at the end of the number by this point, allow this to be parsed as another type
        if (!AtEndOfNumber())
        {
            _i = start;
            return false;
        }

        if (_i > start)
        {
            // repair a number with leading zeros like "00789"
            var num = _text.Substring(start, _i - start);
            var hasInvalidLeadingZero = HasInvalidLeadingZeroRegex().IsMatch(num);

            _output += hasInvalidLeadingZero ? $"\"{num}\"" : num;
            return true;
        }

        return false;
    }

    /**
 * Parse keywords true, false, null
 * Repair Python keywords True, False, None
 */
    private bool ParseKeywords()
    {
        return ParseKeyword("true", "true") ||
               ParseKeyword("false", "false") ||
               ParseKeyword("null", "null") ||
               // repair Python keywords True, False, None
               ParseKeyword("True", "true") ||
               ParseKeyword("False", "false") ||
               ParseKeyword("None", "null");
    }

    private bool ParseKeyword(string name, string value)
    {
        if (_text.Substring(_i, name.Length) == name)
        {
            _output += value;
            _i += name.Length;
            return true;
        }

        return false;
    }

    /**
     * Repair an unquoted string by adding quotes around it
     * Repair a MongoDB function call like NumberLong("2")
     * Repair a JSONP function call like callback({...});
     */
    private bool ParseUnquotedString(bool isKey)
    {
        // note that the symbol can end with whitespaces: we stop at the next delimiter
        // also, note that we allow strings to contain a slash / in order to support repairing regular expressions
        var start = _i;

        if (StringHelper.RegexFunctionNameCharStart().IsMatch(_text[_i].ToString()))
        {
            while (_i < _text.Length && StringHelper.RegexFunctionNameChar().IsMatch(_text[_i].ToString())) _i++;

            var j = _i;
            while (StringHelper.IsWhitespace(_text[j])) j++;

            if (_text[j] == '(')
            {
                // repair a MongoDB function call like NumberLong("2")
                // repair a JSONP function call like callback({...});
                _i = j + 1;

                ParseValue();

                if (_text[_i] == StringHelper.CodeCloseParenthesis)
                {
                    // repair: skip close bracket of function call
                    _i++;
                    if (_text[_i] == StringHelper.CodeSemicolon)
                        // repair: skip semicolon after JSONP call
                        _i++;
                }

                return true;
            }
        }

        while (
            _i < _text.Length &&
            !StringHelper.IsUnquotedStringDelimiter(_text[_i].ToString()) &&
            !StringHelper.IsQuote(_text[_i]) &&
            (!isKey || _text[_i] != StringHelper.CodeColon)
        )
            _i++;

        // test start of an url like "https://..." (this would be parsed as a comment)
        if (_text[_i - 1] == StringHelper.CodeColon &&
            StringHelper.RegexUrlStart().IsMatch(_text.Substring(start, _i + 2 - start)))
            while (_i < _text.Length && StringHelper.RegexUrlChar().IsMatch(_text[_i].ToString()))
                _i++;

        if (_i > start)
        {
            // repair unquoted string
            // also, repair undefined into null

            // first, go back to prevent getting trailing whitespaces in the string
            while (StringHelper.IsWhitespace(_text[_i - 1]) && _i > 0) _i--;

            var symbol = _text.Substring(start, _i - start);
            _output += symbol == "undefined" ? "null" : JsonSerializer.Serialize(symbol);

            if (_text[_i] == StringHelper.CodeDoubleQuote)
                // we had a missing start quote, but now we encountered the end quote, so we can skip that one
                _i++;

            return true;
        }

        return false;
    }

    private bool ParseRegex()
    {
        if (_text[_i] == '/')
        {
            var start = _i;
            _i++;

            while (_i < _text.Length && (_text[_i] != '/' || _text[_i - 1] == '\\')) _i++;

            _i++;

            _output += $"\"{_text.Substring(start, _i - start)}\"";

            return true;
        }

        return false;
    }

    private int PrevNonWhitespaceIndex(int start)
    {
        var prev = start;

        while (prev > 0 && StringHelper.IsWhitespace(_text[prev])) prev--;

        return prev;
    }

    private bool AtEndOfNumber()
    {
        return _i >= _text.Length || StringHelper.IsDelimiter(_text[_i].ToString()) ||
               StringHelper.IsWhitespace(_text[_i]);
    }

    private void RepairNumberEndingWithNumericSymbol(int start)
    {
        // repair numbers cut off at the end
        // this will only be called when we end after a '.', '-', or 'e' and does not
        // change the number more than it needs to make it valid JSON
        _output += $"{_text.Substring(start, _i - start)}0";
    }

    private void ThrowInvalidCharacter(string character)
    {
        throw new JsonRepairError($"Invalid character {JsonSerializer.Serialize(character)}", _i);
    }

    private void ThrowUnexpectedCharacter()
    {
        throw new JsonRepairError($"Unexpected character {JsonSerializer.Serialize(_text[_i].ToString())}", _i);
    }

    private void ThrowUnexpectedEnd()
    {
        throw new JsonRepairError("Unexpected end of json string", _text.Length);
    }

    private void ThrowObjectKeyExpected()
    {
        throw new JsonRepairError("Object key expected", _i);
    }

    private void ThrowColonExpected()
    {
        throw new JsonRepairError("Colon expected", _i);
    }

    private void ThrowInvalidUnicodeCharacter()
    {
        var chars = _text.Substring(_i, Math.Min(6, _text.Length - _i));
        throw new JsonRepairError($"Invalid unicode character \"{chars}\"", _i);
    }

    private static bool AtEndOfBlockComment(string text, int i)
    {
        return i + 1 < text.Length && text[i] == '*' && text[i + 1] == '/';
    }

    [GeneratedRegex("^0\\d")]
    private static partial Regex HasInvalidLeadingZeroRegex();
}