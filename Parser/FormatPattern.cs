using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseTemplate.Parser {
    public class FormatPattern : Pattern {
        static Dictionary<string, string> Formats = new Dictionary<string, string>() {
            ["d"] = "[0-9]+",
            ["f"] = "[-+]?[0-9]*\\.?[0-9]+",
            ["*"] = ".*",
            ["s"] = "\\s*",
            ["S"] = "\\s+",
        };

        public FormatPattern(string format) {
            Format = format;
        }

        public string Format { get; }

        public override string ToRegex() {
            if (Formats.TryGetValue(Format, out var r)) {
                return r;
            } else {
                throw new NotImplementedException($"Unknown format '{Format}'");
            }
        }

        public override string ToString() => $"%{Format}";
    }
}
