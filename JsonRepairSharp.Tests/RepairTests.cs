using JsonRepairSharp.Class;

namespace JsonRepairSharp.Tests;

public class RepairTests
{
    private JsonRepairCore _core = null!;

    [SetUp]
    public void Setup()
    {
        _core = new JsonRepairCore();
    }

    [Test]
    public void RepairMissingQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("abc"), Is.EqualTo("\"abc\""));
            Assert.That(_core.JsonRepair("hello   world"), Is.EqualTo("\"hello   world\""));
            Assert.That(_core.JsonRepair("{\nmessage: hello world\n}"), Is.EqualTo("{\n\"message\": \"hello world\"\n}"));
            Assert.That(_core.JsonRepair("{a:2}"), Is.EqualTo("{\"a\":2}"));
            Assert.That(_core.JsonRepair("{a: 2}"), Is.EqualTo("{\"a\": 2}"));
            Assert.That(_core.JsonRepair("{2: 2}"), Is.EqualTo("{\"2\": 2}"));
            Assert.That(_core.JsonRepair("{true: 2}"), Is.EqualTo("{\"true\": 2}"));
            Assert.That(_core.JsonRepair("{\n  a: 2\n}"), Is.EqualTo("{\n  \"a\": 2\n}"));
            Assert.That(_core.JsonRepair("[a,b]"), Is.EqualTo("[\"a\",\"b\"]"));
            Assert.That(_core.JsonRepair("[\na,\nb\n]"), Is.EqualTo("[\n\"a\",\n\"b\"\n]"));
        });
    }

    [Test]
    public void RepairMissingUrlQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("https://www.bible.com/"), Is.EqualTo("\"https://www.bible.com/\""));
            Assert.That(_core.JsonRepair("{url:https://www.bible.com/}"), Is.EqualTo("{\"url\":\"https://www.bible.com/\"}"));
            Assert.That(_core.JsonRepair("{url:https://www.bible.com/,\"id\":2}"), Is.EqualTo("{\"url\":\"https://www.bible.com/\",\"id\":2}"));
            Assert.That(_core.JsonRepair("[https://www.bible.com/]"), Is.EqualTo("[\"https://www.bible.com/\"]"));
            Assert.That(_core.JsonRepair("[https://www.bible.com/,2]"), Is.EqualTo("[\"https://www.bible.com/\",2]"));
        });
    }

    [Test]
    public void RepairMissingUrlEndQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("\"https://www.bible.com/"), Is.EqualTo("\"https://www.bible.com/\""));
            Assert.That(_core.JsonRepair("{\"url\":\"https://www.bible.com/}"), Is.EqualTo("{\"url\":\"https://www.bible.com/\"}"));
            Assert.That(_core.JsonRepair("{\"url\":\"https://www.bible.com/,\"id\":2}"), Is.EqualTo("{\"url\":\"https://www.bible.com/\",\"id\":2}"));
            Assert.That(_core.JsonRepair("[\"https://www.bible.com/]"), Is.EqualTo("[\"https://www.bible.com/\"]"));
            Assert.That(_core.JsonRepair("[\"https://www.bible.com/,2]"), Is.EqualTo("[\"https://www.bible.com/\",2]"));
        });
    }

    [Test]
    public void RepairMissingEndQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("\"abc"), Is.EqualTo("\"abc\""));
            Assert.That(_core.JsonRepair("'abc"), Is.EqualTo("\"abc\""));

            Assert.That(_core.JsonRepair("\"12:20"), Is.EqualTo("\"12:20\""));
            Assert.That(_core.JsonRepair("{\"time\":\"12:20}"), Is.EqualTo("{\"time\":\"12:20\"}"));
            Assert.That(_core.JsonRepair("{\"date\":2024-10-18T18:35:22.229Z}"), Is.EqualTo("{\"date\":\"2024-10-18T18:35:22.229Z\"}"));
            Assert.That(_core.JsonRepair("\"She said:"), Is.EqualTo("\"She said:\""));
            Assert.That(_core.JsonRepair("{\"text\": \"She said:"), Is.EqualTo("{\"text\": \"She said:\"}"));
            Assert.That(_core.JsonRepair("[\"hello, world]"), Is.EqualTo("[\"hello\", \"world\"]"));
            Assert.That(_core.JsonRepair("[\"hello,\"world\"]"), Is.EqualTo("[\"hello\",\"world\"]"));

            Assert.That(_core.JsonRepair("{\"a\":\"b}"), Is.EqualTo("{\"a\":\"b\"}"));
            Assert.That(_core.JsonRepair("{\"a\":\"b,\"c\":\"d\"}"), Is.EqualTo("{\"a\":\"b\",\"c\":\"d\"}"));
            Assert.That(_core.JsonRepair("{\"a\":\"b,\"c\":\"d\"}"), Is.EqualTo("{\"a\":\"b\",\"c\":\"d\"}"));
            Assert.That(_core.JsonRepair("{\"a\":\"b,c,\"d\":\"e\"}"), Is.EqualTo("{\"a\":\"b,c\",\"d\":\"e\"}"));
            Assert.That(_core.JsonRepair("{a:\"b,c,\"d\":\"e\"}"), Is.EqualTo("{\"a\":\"b,c\",\"d\":\"e\"}"));
            // Assert.That(_core.JsonRepair("{a:\"b,c,}"), Is.EqualTo("{\"a\":\"b,c\"}") // TODO: support this case
            Assert.That(_core.JsonRepair("[\"b,c,]"), Is.EqualTo("[\"b\",\"c\"]"));

            Assert.That(_core.JsonRepair("\u2018abc"), Is.EqualTo("\"abc\""));
            Assert.That(_core.JsonRepair("\"it's working"), Is.EqualTo("\"it's working\""));
            Assert.That(_core.JsonRepair("[\"abc+/*comment*/\"def\"]"), Is.EqualTo("[\"abcdef\"]"));
            Assert.That(_core.JsonRepair("[\"abc/*comment*/+\"def\"]"), Is.EqualTo("[\"abcdef\"]"));
            Assert.That(_core.JsonRepair("[\"abc,/*comment*/\"def\"]"), Is.EqualTo("[\"abc\",\"def\"]"));
        });
    }

    [Test]
    public void RepairTruncatedJsonTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("\"foo"), Is.EqualTo("\"foo\""));
            Assert.That(_core.JsonRepair("["), Is.EqualTo("[]"));
            Assert.That(_core.JsonRepair("[\"foo"), Is.EqualTo("[\"foo\"]"));
            Assert.That(_core.JsonRepair("[\"foo\""), Is.EqualTo("[\"foo\"]"));
            Assert.That(_core.JsonRepair("[\"foo\","), Is.EqualTo("[\"foo\"]"));
            Assert.That(_core.JsonRepair("{\"foo\":\"bar\""), Is.EqualTo("{\"foo\":\"bar\"}"));
            Assert.That(_core.JsonRepair("{\"foo\":\"bar"), Is.EqualTo("{\"foo\":\"bar\"}"));
            Assert.That(_core.JsonRepair("{\"foo\":"), Is.EqualTo("{\"foo\":null}"));
            Assert.That(_core.JsonRepair("{\"foo\""), Is.EqualTo("{\"foo\":null}"));
            Assert.That(_core.JsonRepair("{\"foo"), Is.EqualTo("{\"foo\":null}"));
            Assert.That(_core.JsonRepair("{"), Is.EqualTo("{}"));
            Assert.That(_core.JsonRepair("2."), Is.EqualTo("2.0"));
            Assert.That(_core.JsonRepair("2e"), Is.EqualTo("2e0"));
            Assert.That(_core.JsonRepair("2e+"), Is.EqualTo("2e+0"));
            Assert.That(_core.JsonRepair("2e-"), Is.EqualTo("2e-0"));
            Assert.That(_core.JsonRepair("{\"foo\":\"bar\\u20"), Is.EqualTo("{\"foo\":\"bar\"}"));
            Assert.That(_core.JsonRepair("\"\\u"), Is.EqualTo("\"\""));
            Assert.That(_core.JsonRepair("\"\\u2"), Is.EqualTo("\"\""));
            Assert.That(_core.JsonRepair("\"\\u260"), Is.EqualTo("\"\""));
            Assert.That(_core.JsonRepair("\"\\u2605"), Is.EqualTo("\"\\u2605\""));
            Assert.That(_core.JsonRepair("{\"s \\ud"), Is.EqualTo("{\"s\": null}"));
            Assert.That(_core.JsonRepair("{\"message\": \"it's working"), Is.EqualTo("{\"message\": \"it's working\"}"));
            Assert.That(_core.JsonRepair("{\"text\":\"Hello Sergey,I hop"), Is.EqualTo("{\"text\":\"Hello Sergey,I hop\"}"));
            Assert.That(_core.JsonRepair("{\"message\": \"with, multiple, commma's, you see?"), Is.EqualTo("{\"message\": \"with, multiple, commma's, you see?\"}"));
        });
    }

    [Test]
    public void RepairEllipsisInArrayTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("[1,2,3,...]"), Is.EqualTo("[1,2,3]"));
            Assert.That(_core.JsonRepair("[1, 2, 3, ... ]"), Is.EqualTo("[1, 2, 3  ]"));
            Assert.That(_core.JsonRepair("[1,2,3,/*comment1*/.../*comment2*/]"), Is.EqualTo("[1,2,3]"));
            Assert.That(_core.JsonRepair("[\n  1,\n  2,\n  3,\n  /*comment1*/  .../*comment2*/\n]"), Is.EqualTo("[\n  1,\n  2,\n  3\n    \n]"));
            Assert.That(_core.JsonRepair("{\"array\":[1,2,3,...]}"), Is.EqualTo("{\"array\":[1,2,3]}"));
            Assert.That(_core.JsonRepair("[1,2,3,...,9]"), Is.EqualTo("[1,2,3,9]"));
            Assert.That(_core.JsonRepair("[...,7,8,9]"), Is.EqualTo("[7,8,9]"));
            Assert.That(_core.JsonRepair("[..., 7,8,9]"), Is.EqualTo("[ 7,8,9]"));
            Assert.That(_core.JsonRepair("[...]"), Is.EqualTo("[]"));
            Assert.That(_core.JsonRepair("[ ... ]"), Is.EqualTo("[  ]"));
        });
    }

    [Test]
    public void RepairEllipsisInObjectTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("{\"a\":2,\"b\":3,...}"), Is.EqualTo("{\"a\":2,\"b\":3}"));
            Assert.That(_core.JsonRepair("{\"a\":2,\"b\":3,/*comment1*/.../*comment2*/}"), Is.EqualTo("{\"a\":2,\"b\":3}"));
            Assert.That(_core.JsonRepair("{\n  \"a\":2,\n  \"b\":3,\n  /*comment1*/.../*comment2*/\n}"), Is.EqualTo("{\n  \"a\":2,\n  \"b\":3\n  \n}"));
            Assert.That(_core.JsonRepair("{\"a\":2,\"b\":3, ... }"), Is.EqualTo("{\"a\":2,\"b\":3  }"));
            Assert.That(_core.JsonRepair("{\"nested\":{\"a\":2,\"b\":3, ... }}"), Is.EqualTo("{\"nested\":{\"a\":2,\"b\":3  }}"));
            Assert.That(_core.JsonRepair("{\"a\":2,\"b\":3,...,\"z\":26}"), Is.EqualTo("{\"a\":2,\"b\":3,\"z\":26}"));
            Assert.That(_core.JsonRepair("{\"a\":2,\"b\":3,...}"), Is.EqualTo("{\"a\":2,\"b\":3}"));
            Assert.That(_core.JsonRepair("{...}"), Is.EqualTo("{}"));
            Assert.That(_core.JsonRepair("{ ... }"), Is.EqualTo("{  }"));
        });
    }

    [Test]
    public void RepairStartQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_core.JsonRepair("abc\""), Is.EqualTo("\"abc\""));
            Assert.That(_core.JsonRepair("[a\",\"b\"]"), Is.EqualTo("[\"a\",\"b\"]"));
            Assert.That(_core.JsonRepair("[a\",b\"]"), Is.EqualTo("[\"a\",\"b\"]"));
            Assert.That(_core.JsonRepair("{\"a\":\"foo\",\"b\":\"bar\"}"), Is.EqualTo("{\"a\":\"foo\",\"b\":\"bar\"}"));
            Assert.That(_core.JsonRepair("{a\":\"foo\",\"b\":\"bar\"}"), Is.EqualTo("{\"a\":\"foo\",\"b\":\"bar\"}"));
            Assert.That(_core.JsonRepair("{\"a\":\"foo\",b\":\"bar\"}"), Is.EqualTo("{\"a\":\"foo\",\"b\":\"bar\"}"));
            Assert.That(_core.JsonRepair("{\"a\":foo\",\"b\":\"bar\"}"), Is.EqualTo("{\"a\":\"foo\",\"b\":\"bar\"}"));
        });
    }

    [Test]
    public void RepairMultipleLineValueTest()
    {
        const string raw = """
                           {
                             "x": "1234
                           123123123"
                           }
                           """;

        const string expected = """
                                {
                                  "x": "1234\r\n123123123"
                                }
                                """;

        Assert.That(_core.JsonRepair(raw), Is.EqualTo(expected));
    }
}