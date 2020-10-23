using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseTemplate.Parser {
    public class FormatPattern : Pattern {
        public FormatPattern(string format) {
            Format = format;
        }

        public string Format { get; }

        public override string ToRegex() {
            switch (Format) {
                case "d":
                    return "[0-9]+";
                case "f":
                    return "[-+]?[0-9]*\\.?[0-9]+";
                default:
                    throw new NotImplementedException($"Unknown format '{Format}'");
            }
        }

        public override string ToString() => $"%{Format}";
    }
}
