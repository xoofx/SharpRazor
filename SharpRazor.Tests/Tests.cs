using NUnit.Framework;

namespace SharpRazor.Tests
{
    [TestFixture]
    public class Tests
    {
        private Razorizer razorizer;

        [TestFixtureSetUp]
        public void Setup()
        {
            razorizer = new Razorizer();
        }

        [Test]
        public void TestSimpleWithModel()
        {
            var test = razorizer.Parse("<p>Hello @Model!</p>", "Razor");
            Assert.AreEqual(test, "<p>Hello Razor!</p>");
        }

        [Test]
        public void TestSimpleWithAnonymousModel()
        {
            var test = razorizer.Parse("<p>Hello @Model.Name!</p>", new { Name = "Razor" });
            Assert.AreEqual(test, "<p>Hello Razor!</p>");
        }

        public class MyCustomModel
        {
            public string Name { get; set; }
        }

        [Test]
        public void TestSimpleWithCustomClass()
        {
            var test = razorizer.Parse("<p>Hello @Model.Name!</p>", new MyCustomModel { Name = "Razor" });
            Assert.AreEqual(test, "<p>Hello Razor!</p>");
        }
    }
}
