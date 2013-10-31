using System;
using System.Collections.Generic;
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

        [Test]
        public void TestLayout()
        {
            var layout = @"@{   
    this.Layout = @""layoutRendering"";
}
@section Head
{<head>
    <title>@Model[""mykey""]</title>
</head>}
@section Body
{<body>
my body
</body>}
";

            var layoutRendering = @"<!DOCTYPE html>
<html>
@RenderSection(""Head"")
@RenderSection(""Body"")
</html>
";
            // Create razorizer
            var razorizer = new Razorizer {EnableDebug = true};

            var model = new Dictionary<string, object>() {{"mykey", "This is the title"}};

            // Pre-generate the layoutRendering page (and cache it using its name)
            var layoutRenderingPage = razorizer.Compile("layoutRendering", layoutRendering,
                typeof (IDictionary<string, object>));

            // Run the layout (using indirectly the previous page compiled)
            var result = razorizer.Parse(layout, model);

            Assert.AreEqual(@"<!DOCTYPE html>
<html>
<head>
    <title>This is the title</title>
</head>
<body>
my body
</body>
</html>
", result);
        }

    }
}
