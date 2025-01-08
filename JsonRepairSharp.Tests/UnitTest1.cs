using JsonRepairSharp.Class;

namespace JsonRepairSharp.Tests
{
    public class Tests
    {
        private JsonRepairCore _core = null!;

        [SetUp]
        public void Setup()
        {
            _core = new JsonRepairCore();
        }

        [Test]
        public void SimpleTest()
        {
            var repaired = _core.JsonRepair("[https://www.bible.com/]");

            Assert.That(repaired, Is.EqualTo("[\"https://www.bible.com/\"]"));
        }
    }
}
