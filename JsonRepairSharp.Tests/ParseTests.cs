using JsonRepairSharp.Class;

namespace JsonRepairSharp.Tests;

public class ParseTests
{
    private JsonRepairCore _core = null!;

    [SetUp]
    public void Setup()
    {
        _core = new JsonRepairCore();
    }

    [Test]
    public void ParseWhitespaceTest()
    {
        AssertRepair("  { \n } \t ");
    }

    [Test]
    public void ParseObjectTest()
    {
        AssertRepair("{}");
        AssertRepair("{  }");
        AssertRepair("{\"a\": {}}");
        AssertRepair("{\"a\": \"b\"}");
        AssertRepair("{\"a\": 2}");
    }

    [Test]
    public void ParseArrayTest()
    {
        AssertRepair("[]");
        AssertRepair("[  ]");
        AssertRepair("[1,2,3]");
        AssertRepair("[ 1 , 2 , 3 ]");
        AssertRepair("[1,2,[3,4,5]]");
        AssertRepair("[{}]");
        AssertRepair("{\"a\":[]}");
        AssertRepair("[1, \"hi\", true, false, null, {}, []]");
    }

    [Test]
    public void ParseNumberTest()
    {
        AssertRepair("23");
        AssertRepair("0");
        AssertRepair("0e+2");
        AssertRepair("0.0");
        AssertRepair("-0");
        AssertRepair("2.3");
        AssertRepair("2300e3");
        AssertRepair("2300e+3");
        AssertRepair("2300e-3");
        AssertRepair("-2");
        AssertRepair("2e-3");
        AssertRepair("2.3e-3");
    }

    [Test]
    public void ParseStringTest()
    {
        AssertRepair("\"str\"");
        AssertRepair("\"\\\"\\\\\\\\\\\\b\\f\\n\\r\\t\"");
        AssertRepair("\"\\u260E\"");
    }

    [Test]
    public void ParseKeywordsTest()
    {
        AssertRepair("true");
        AssertRepair("false");
        AssertRepair("null");
    }

    [Test]
    public void ParseDelimiterTest()
    {
        AssertRepair("\"\"");
        AssertRepair("\"[\"");
        AssertRepair("\"]\"");
        AssertRepair("\"{\"");
        AssertRepair("\"}\"");
        AssertRepair("\":\"");
        AssertRepair("\",\"");
    }

    [Test]
    public void ParseUnicodeInStringTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("\"★\""), Is.EqualTo("\"★\""));
            Assert.That(_core.JsonRepair("\"\u2605\""), Is.EqualTo("\"\u2605\""));
            Assert.That(_core.JsonRepair("\"😀\""), Is.EqualTo("\"😀\""));
            Assert.That(_core.JsonRepair("\"\ud83d\ude00\""), Is.EqualTo("\"\ud83d\ude00\""));
            Assert.That(_core.JsonRepair("\"йнформация\""), Is.EqualTo("\"йнформация\""));
        });
    }

    [Test]
    public void ParseEscapedUnicodeInStringTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("\"\\u2605\""), Is.EqualTo("\"\\u2605\""));
            Assert.That(_core.JsonRepair("\"\\u2605A\""), Is.EqualTo("\"\\u2605A\""));
            Assert.That(_core.JsonRepair("\"\\ud83d\\ude00\""), Is.EqualTo("\"\\ud83d\\ude00\""));
            Assert.That(_core.JsonRepair("\"\\u0439\\u043d\\u0444\\u043e\\u0440\\u043c\\u0430\\u0446\\u0438\\u044f\""),
                Is.EqualTo("\"\\u0439\\u043d\\u0444\\u043e\\u0440\\u043c\\u0430\\u0446\\u0438\\u044f\""));
        });
    }

    [Test]
    public void ParseUnicodeInKeyTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("{\"★\":true}"), Is.EqualTo("{\"★\":true}"));
            Assert.That(_core.JsonRepair("{\"\u2605\":true}"), Is.EqualTo("{\"\u2605\":true}"));
            Assert.That(_core.JsonRepair("{\"😀\":true}"), Is.EqualTo("{\"😀\":true}"));
            Assert.That(_core.JsonRepair("{\"\ud83d\ude00\":true}"), Is.EqualTo("{\"\ud83d\ude00\":true}"));
        });
    }

    private void AssertRepair(string text)
    {
        Assert.That(_core.JsonRepair(text), Is.EqualTo(text));
    }
}