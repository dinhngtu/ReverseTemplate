using ReverseTemplate.Engine;
using System;
using Xunit;

using static ReverseTemplate.Tests.TestUtils;

namespace ReverseTemplate.Tests {
    public class ParserTests {
        [Fact]
        public void SimpleTest() {
            var line = "aaa {{/pattern/=capture}} bbb";
            var outLine = AssertParses(line);
            Assert.Equal(3, outLine.Sections.Count);
            outLine.AssertIsText(0, "aaa ");
            outLine.AssertIsCapture(1, "{{/pattern/=capture}}");
            outLine.AssertIsText(2, " bbb");
        }

        [Fact]
        public void TextOnlyTest() {
            var line = "aaa bbb ccc";
            var outLine = AssertParses(line);
            Assert.Single(outLine.Sections);
            outLine.AssertIsText(0, "aaa bbb ccc");
        }

        [Fact]
        public void CaptureOnlyTest() {
            var line = "{{/pattern/=capture}}";
            var outLine = AssertParses(line);
            Assert.Single(outLine.Sections);
            outLine.AssertIsCapture(0, "{{/pattern/=capture}}");
        }

        [Fact]
        public void NonCaptureTest() {
            var line = "{{/pattern/}}";
            var outLine = AssertParses(line);
            Assert.Single(outLine.Sections);
            outLine.AssertIsCapture(0, "{{/pattern/}}");
        }

        [Fact]
        public void MultipleCapturesTest() {
            var line = "{{/p1/=c1}} aaa {{%f=c2}}";
            var outLine = AssertParses(line);
            Assert.Equal(3, outLine.Sections.Count);
            outLine.AssertIsCapture(0);
            outLine.AssertIsText(1, " aaa ");
            outLine.AssertIsCapture(2);
        }

        [Fact]
        public void CaptureNamesTest() {
            var line = "{{/p1/=c1}} aaa {{%f=c2}}";
            var outLine = new CachedTemplateLine(AssertParses(line));
            Assert.Equal(2, outLine.CaptureNames.Count);
            Assert.Contains("c1", outLine.CaptureNames);
            Assert.Contains("c2", outLine.CaptureNames);
        }

        [Fact]
        public void ConsecutiveCapturesTest() {
            var line = "{{/p1/=c1}}{{%f=c2}}";
            var outLine = AssertParses(line);
            Assert.Equal(2, outLine.Sections.Count);
            outLine.AssertIsCapture(0, "{{/p1/=c1}}");
            outLine.AssertIsCapture(1, "{{%f=c2}}");
        }

        [Fact]
        public void BracketsTest() {
            var line = "aaa {bbb} {ccc{ }ddd} eee";
            var outLine = AssertParses(line);
            Assert.Single(outLine.Sections);
            outLine.AssertIsText(0, "aaa {bbb} {ccc{ }ddd} eee");
        }

        [Fact]
        public void BeginningBracketTest() {
            var line = "{aaa";
            var outLine = AssertParses(line);
            Assert.Single(outLine.Sections);
            outLine.AssertIsText(0, "{aaa");
        }

        [Fact]
        public void EndingBracketTest() {
            var line = "aaa}";
            var outLine = AssertParses(line);
            Assert.Single(outLine.Sections);
            outLine.AssertIsText(0, "aaa}");
        }

        [Fact]
        public void WeirdBracketTest() {
            var line = "}aaa{";
            var outLine = AssertParses(line);
            Assert.Single(outLine.Sections);
            outLine.AssertIsText(0, "}aaa{");
        }

        [Fact]
        public void EmptyLineTest() {
            var line = "";
            var outLine = AssertParses(line);
            Assert.Empty(outLine.Sections);
        }

        [Fact]
        public void EscapeTest() {
            var line = @"{{/asd\/asd/}}";
            var outLine = AssertParses(line);
            outLine.AssertIsCapture(0, @"{{/asd\/asd/}}");
        }

        [Fact]
        public void RegexEscapeTest() {
            var line = @"{{/asd\+/}}";
            var outLine = AssertParses(line);
            outLine.AssertIsCapture(0, @"{{/asd\+/}}");
        }
    }
}
