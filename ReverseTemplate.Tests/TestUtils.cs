// SPDX-License-Identifier: GPL-3.0-only

using ReverseTemplate.Engine;
using ReverseTemplate.Parser;
using Superpower;
using System.IO;
using Xunit;

namespace ReverseTemplate.Tests {
    public static class TestUtils {
        public static TemplateLine AssertParses(string line) {
            return TemplateFileParser.TemplateLine.Parse(line);
        }

        public static void AssertDoesntParse(string line) {
            var result = TemplateFileParser.TemplateLine.TryParse(line);
            Assert.False(result.HasValue);
        }

        public static TextSection AssertIsText(this TemplateLine line, int idx) {
            Assert.IsType<TextSection>(line.Sections[idx]);
            return (TextSection)line.Sections[idx];
        }

        public static TextSection AssertIsText(this TemplateLine line, int idx, string content) {
            Assert.IsType<TextSection>(line.Sections[idx]);
            Assert.Equal(content, line.Sections[idx].ToString());
            return (TextSection)line.Sections[idx];
        }

        public static CaptureSection AssertIsCapture(this TemplateLine line, int idx) {
            Assert.IsType<CaptureSection>(line.Sections[idx]);
            return (CaptureSection)line.Sections[idx];
        }

        public static Template MakeTemplate(string template) {
            using var reader = new StringReader(template);
            return Template.Create(reader);
        }
    }
}
