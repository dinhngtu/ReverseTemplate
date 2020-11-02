using ReverseTemplate.Engine;
using ReverseTemplate.Parser;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace ReverseTemplate.Tests {
    public static class TestUtils {
        public static TemplateLine AssertParses(string line) {
            Assert.True(TemplateParser.TryParse(line, out var outLine, out var error, out var errorPosition));
            Assert.Null(error);
            Assert.Equal(Position.Empty, errorPosition);
            Assert.NotNull(outLine);
            return outLine!;
        }

        public static void AssertDoesntParse(string line) {
            Assert.False(TemplateParser.TryParse(line, out _, out _, out _));
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

        public static LineSection AssertIsCapture(this TemplateLine line, int idx) {
            Assert.IsType<CaptureSection>(line.Sections[idx]);
            return (CaptureSection)line.Sections[idx];
        }

        public static LineSection AssertIsCapture(this TemplateLine line, int idx, string text) {
            Assert.IsType<CaptureSection>(line.Sections[idx]);
            Assert.Equal(text, line.Sections[idx].ToString());
            return (CaptureSection)line.Sections[idx];
        }

        static IEnumerable<TemplateLine> ParseLines(TextReader reader) {
            string? l;
            while ((l = reader.ReadLine()) != null) {
                yield return AssertParses(l);
            }
        }

        public static Template MakeTemplate(string template) {
            using var reader = new StringReader(template);
            return new Template(ParseLines(reader));
        }
    }
}
