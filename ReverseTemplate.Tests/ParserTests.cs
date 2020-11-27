// SPDX-License-Identifier: GPL-3.0-only

using ReverseTemplate.Engine;
using ReverseTemplate.Parser;
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
            var capture = outLine.AssertIsCapture(1);
            Assert.IsType<RegexPattern>(capture.Pattern);
            Assert.Single(capture.VarPath, x => x.Name == "capture");
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
            var capture = outLine.AssertIsCapture(0);
            Assert.IsType<RegexPattern>(capture.Pattern);
            Assert.Single(capture.VarPath, x => x.Name == "capture");
        }

        [Fact]
        public void NonCaptureTest() {
            var line = "{{/pattern/}}";
            var outLine = AssertParses(line);
            var capture = Assert.IsType<CaptureSection>(Assert.Single(outLine.Sections));
            Assert.IsType<RegexPattern>(capture.Pattern);
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
            Assert.Equal(2, outLine.Captures.Count);
            Assert.Contains(outLine.Captures, x => x.VarName == "c1");
            Assert.Contains(outLine.Captures, x => x.VarName == "c2");
        }

        [Fact]
        public void ConsecutiveCapturesTest() {
            var line = "{{/p1/=c1}}{{%f=c2}}";
            var outLine = AssertParses(line);
            Assert.Equal(2, outLine.Sections.Count);
            var c1 = outLine.AssertIsCapture(0);
            Assert.IsType<RegexPattern>(c1.Pattern);
            Assert.Single(c1.VarPath, x => x.Name == "c1");
            var c2 = outLine.AssertIsCapture(1);
            var f2 = Assert.IsType<FormatPattern>(c2.Pattern);
            Assert.Equal("f", f2.Format);
            Assert.Single(c2.VarPath, x => x.Name == "c2");
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
            var capture = outLine.AssertIsCapture(0);
            Assert.Equal(@"(?:asd/asd)", new CachedCaptureSection(capture).ToRegex());
        }

        [Fact]
        public void RegexEscapeTest() {
            var line = @"{{/asd\+/}}";
            var outLine = AssertParses(line);
            var capture = outLine.AssertIsCapture(0);
            Assert.Equal(@"(?:asd\+)", new CachedCaptureSection(capture).ToRegex());
        }
    }
}
