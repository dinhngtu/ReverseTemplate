// SPDX-License-Identifier: GPL-3.0-only

using ReverseTemplate.Engine;
using ReverseTemplate.Parser;
using Sprache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace ReverseTemplate.Tests {
    public static class TestUtils {
        public static TemplateLine AssertParses(string line) {
            var result = TemplateFileParser.TemplateLine.TryParse(line);
            Assert.True(result.WasSuccessful);
            return result.Value;
        }

        public static void AssertDoesntParse(string line) {
            var result = TemplateFileParser.TemplateLine.TryParse(line);
            Assert.False(result.WasSuccessful);
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

        static IEnumerable<TemplateLine> ParseLines(TextReader reader) {
            string? l;
            while ((l = reader.ReadLine()) != null) {
                yield return AssertParses(l);
            }
        }

        public static Template MakeTemplate(string template) {
            using var reader = new StringReader(template);
            return Template.Create(reader);
        }
    }
}
