using JsonRepairSharp.Class;

namespace JsonRepairSharp.Tests;

public class RepairTests
{
    [Test]
    public void RepairMissingQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonRepairCore.JsonRepair("abc"), Is.EqualTo("\"abc\""));
            Assert.That(JsonRepairCore.JsonRepair("hello   world"), Is.EqualTo("\"hello   world\""));
            Assert.That(JsonRepairCore.JsonRepair("{\nmessage: hello world\n}"), Is.EqualTo("{\n\"message\": \"hello world\"\n}"));
            Assert.That(JsonRepairCore.JsonRepair("{a:2}"), Is.EqualTo("{\"a\":2}"));
            Assert.That(JsonRepairCore.JsonRepair("{a: 2}"), Is.EqualTo("{\"a\": 2}"));
            Assert.That(JsonRepairCore.JsonRepair("{2: 2}"), Is.EqualTo("{\"2\": 2}"));
            Assert.That(JsonRepairCore.JsonRepair("{true: 2}"), Is.EqualTo("{\"true\": 2}"));
            Assert.That(JsonRepairCore.JsonRepair("{\n  a: 2\n}"), Is.EqualTo("{\n  \"a\": 2\n}"));
            Assert.That(JsonRepairCore.JsonRepair("[a,b]"), Is.EqualTo("[\"a\",\"b\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[\na,\nb\n]"), Is.EqualTo("[\n\"a\",\n\"b\"\n]"));
        });
    }

    [Test]
    public void RepairMissingUrlQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonRepairCore.JsonRepair("https://www.bible.com/"), Is.EqualTo("\"https://www.bible.com/\""));
            Assert.That(JsonRepairCore.JsonRepair("{url:https://www.bible.com/}"), Is.EqualTo("{\"url\":\"https://www.bible.com/\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{url:https://www.bible.com/,\"id\":2}"), Is.EqualTo("{\"url\":\"https://www.bible.com/\",\"id\":2}"));
            Assert.That(JsonRepairCore.JsonRepair("[https://www.bible.com/]"), Is.EqualTo("[\"https://www.bible.com/\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[https://www.bible.com/,2]"), Is.EqualTo("[\"https://www.bible.com/\",2]"));
        });
    }

    [Test]
    public void RepairMissingUrlEndQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonRepairCore.JsonRepair("\"https://www.bible.com/"), Is.EqualTo("\"https://www.bible.com/\""));
            Assert.That(JsonRepairCore.JsonRepair("{\"url\":\"https://www.bible.com/}"), Is.EqualTo("{\"url\":\"https://www.bible.com/\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"url\":\"https://www.bible.com/,\"id\":2}"), Is.EqualTo("{\"url\":\"https://www.bible.com/\",\"id\":2}"));
            Assert.That(JsonRepairCore.JsonRepair("[\"https://www.bible.com/]"), Is.EqualTo("[\"https://www.bible.com/\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[\"https://www.bible.com/,2]"), Is.EqualTo("[\"https://www.bible.com/\",2]"));
        });
    }

    [Test]
    public void RepairMissingEndQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonRepairCore.JsonRepair("\"abc"), Is.EqualTo("\"abc\""));
            Assert.That(JsonRepairCore.JsonRepair("'abc"), Is.EqualTo("\"abc\""));

            Assert.That(JsonRepairCore.JsonRepair("\"12:20"), Is.EqualTo("\"12:20\""));
            Assert.That(JsonRepairCore.JsonRepair("{\"time\":\"12:20}"), Is.EqualTo("{\"time\":\"12:20\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"date\":2024-10-18T18:35:22.229Z}"), Is.EqualTo("{\"date\":\"2024-10-18T18:35:22.229Z\"}"));
            Assert.That(JsonRepairCore.JsonRepair("\"She said:"), Is.EqualTo("\"She said:\""));
            Assert.That(JsonRepairCore.JsonRepair("{\"text\": \"She said:"), Is.EqualTo("{\"text\": \"She said:\"}"));
            Assert.That(JsonRepairCore.JsonRepair("[\"hello, world]"), Is.EqualTo("[\"hello\", \"world\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[\"hello,\"world\"]"), Is.EqualTo("[\"hello\",\"world\"]"));

            Assert.That(JsonRepairCore.JsonRepair("{\"a\":\"b}"), Is.EqualTo("{\"a\":\"b\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":\"b,\"c\":\"d\"}"), Is.EqualTo("{\"a\":\"b\",\"c\":\"d\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":\"b,\"c\":\"d\"}"), Is.EqualTo("{\"a\":\"b\",\"c\":\"d\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":\"b,c,\"d\":\"e\"}"), Is.EqualTo("{\"a\":\"b,c\",\"d\":\"e\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{a:\"b,c,\"d\":\"e\"}"), Is.EqualTo("{\"a\":\"b,c\",\"d\":\"e\"}"));
            // Assert.That(JsonRepairCore.JsonRepair("{a:\"b,c,}"), Is.EqualTo("{\"a\":\"b,c\"}") // TODO: support this case
            Assert.That(JsonRepairCore.JsonRepair("[\"b,c,]"), Is.EqualTo("[\"b\",\"c\"]"));

            Assert.That(JsonRepairCore.JsonRepair("\u2018abc"), Is.EqualTo("\"abc\""));
            Assert.That(JsonRepairCore.JsonRepair("\"it's working"), Is.EqualTo("\"it's working\""));
            Assert.That(JsonRepairCore.JsonRepair("[\"abc+/*comment*/\"def\"]"), Is.EqualTo("[\"abcdef\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[\"abc/*comment*/+\"def\"]"), Is.EqualTo("[\"abcdef\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[\"abc,/*comment*/\"def\"]"), Is.EqualTo("[\"abc\",\"def\"]"));
        });
    }

    [Test]
    public void RepairTruncatedJsonTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonRepairCore.JsonRepair("\"foo"), Is.EqualTo("\"foo\""));
            Assert.That(JsonRepairCore.JsonRepair("["), Is.EqualTo("[]"));
            Assert.That(JsonRepairCore.JsonRepair("[\"foo"), Is.EqualTo("[\"foo\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[\"foo\""), Is.EqualTo("[\"foo\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[\"foo\","), Is.EqualTo("[\"foo\"]"));
            Assert.That(JsonRepairCore.JsonRepair("{\"foo\":\"bar\""), Is.EqualTo("{\"foo\":\"bar\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"foo\":\"bar"), Is.EqualTo("{\"foo\":\"bar\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"foo\":"), Is.EqualTo("{\"foo\":null}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"foo\""), Is.EqualTo("{\"foo\":null}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"foo"), Is.EqualTo("{\"foo\":null}"));
            Assert.That(JsonRepairCore.JsonRepair("{"), Is.EqualTo("{}"));
            Assert.That(JsonRepairCore.JsonRepair("2."), Is.EqualTo("2.0"));
            Assert.That(JsonRepairCore.JsonRepair("2e"), Is.EqualTo("2e0"));
            Assert.That(JsonRepairCore.JsonRepair("2e+"), Is.EqualTo("2e+0"));
            Assert.That(JsonRepairCore.JsonRepair("2e-"), Is.EqualTo("2e-0"));
            Assert.That(JsonRepairCore.JsonRepair("{\"foo\":\"bar\\u20"), Is.EqualTo("{\"foo\":\"bar\"}"));
            Assert.That(JsonRepairCore.JsonRepair("\"\\u"), Is.EqualTo("\"\""));
            Assert.That(JsonRepairCore.JsonRepair("\"\\u2"), Is.EqualTo("\"\""));
            Assert.That(JsonRepairCore.JsonRepair("\"\\u260"), Is.EqualTo("\"\""));
            Assert.That(JsonRepairCore.JsonRepair("\"\\u2605"), Is.EqualTo("\"\\u2605\""));
            Assert.That(JsonRepairCore.JsonRepair("{\"s \\ud"), Is.EqualTo("{\"s\": null}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"message\": \"it's working"), Is.EqualTo("{\"message\": \"it's working\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"text\":\"Hello Sergey,I hop"), Is.EqualTo("{\"text\":\"Hello Sergey,I hop\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"message\": \"with, multiple, commma's, you see?"), Is.EqualTo("{\"message\": \"with, multiple, commma's, you see?\"}"));
        });
    }

    [Test]
    public void RepairEllipsisInArrayTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonRepairCore.JsonRepair("[1,2,3,...]"), Is.EqualTo("[1,2,3]"));
            Assert.That(JsonRepairCore.JsonRepair("[1, 2, 3, ... ]"), Is.EqualTo("[1, 2, 3  ]"));
            Assert.That(JsonRepairCore.JsonRepair("[1,2,3,/*comment1*/.../*comment2*/]"), Is.EqualTo("[1,2,3]"));
            Assert.That(JsonRepairCore.JsonRepair("[\n  1,\n  2,\n  3,\n  /*comment1*/  .../*comment2*/\n]"), Is.EqualTo("[\n  1,\n  2,\n  3\n    \n]"));
            Assert.That(JsonRepairCore.JsonRepair("{\"array\":[1,2,3,...]}"), Is.EqualTo("{\"array\":[1,2,3]}"));
            Assert.That(JsonRepairCore.JsonRepair("[1,2,3,...,9]"), Is.EqualTo("[1,2,3,9]"));
            Assert.That(JsonRepairCore.JsonRepair("[...,7,8,9]"), Is.EqualTo("[7,8,9]"));
            Assert.That(JsonRepairCore.JsonRepair("[..., 7,8,9]"), Is.EqualTo("[ 7,8,9]"));
            Assert.That(JsonRepairCore.JsonRepair("[...]"), Is.EqualTo("[]"));
            Assert.That(JsonRepairCore.JsonRepair("[ ... ]"), Is.EqualTo("[  ]"));
        });
    }

    [Test]
    public void RepairEllipsisInObjectTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":2,\"b\":3,...}"), Is.EqualTo("{\"a\":2,\"b\":3}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":2,\"b\":3,/*comment1*/.../*comment2*/}"), Is.EqualTo("{\"a\":2,\"b\":3}"));
            Assert.That(JsonRepairCore.JsonRepair("{\n  \"a\":2,\n  \"b\":3,\n  /*comment1*/.../*comment2*/\n}"), Is.EqualTo("{\n  \"a\":2,\n  \"b\":3\n  \n}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":2,\"b\":3, ... }"), Is.EqualTo("{\"a\":2,\"b\":3  }"));
            Assert.That(JsonRepairCore.JsonRepair("{\"nested\":{\"a\":2,\"b\":3, ... }}"), Is.EqualTo("{\"nested\":{\"a\":2,\"b\":3  }}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":2,\"b\":3,...,\"z\":26}"), Is.EqualTo("{\"a\":2,\"b\":3,\"z\":26}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":2,\"b\":3,...}"), Is.EqualTo("{\"a\":2,\"b\":3}"));
            Assert.That(JsonRepairCore.JsonRepair("{...}"), Is.EqualTo("{}"));
            Assert.That(JsonRepairCore.JsonRepair("{ ... }"), Is.EqualTo("{  }"));
        });
    }

    [Test]
    public void RepairStartQuoteTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JsonRepairCore.JsonRepair("abc\""), Is.EqualTo("\"abc\""));
            Assert.That(JsonRepairCore.JsonRepair("[a\",\"b\"]"), Is.EqualTo("[\"a\",\"b\"]"));
            Assert.That(JsonRepairCore.JsonRepair("[a\",b\"]"), Is.EqualTo("[\"a\",\"b\"]"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":\"foo\",\"b\":\"bar\"}"), Is.EqualTo("{\"a\":\"foo\",\"b\":\"bar\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{a\":\"foo\",\"b\":\"bar\"}"), Is.EqualTo("{\"a\":\"foo\",\"b\":\"bar\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":\"foo\",b\":\"bar\"}"), Is.EqualTo("{\"a\":\"foo\",\"b\":\"bar\"}"));
            Assert.That(JsonRepairCore.JsonRepair("{\"a\":foo\",\"b\":\"bar\"}"), Is.EqualTo("{\"a\":\"foo\",\"b\":\"bar\"}"));
        });
    }
}