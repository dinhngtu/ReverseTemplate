using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseTemplate.Parser {
    public class CaptureSection : LineSection {
        public CaptureSection(Pattern pattern, string? varName) {
            Pattern = pattern;
            VarName = varName;
        }

        public Pattern Pattern { get; }
        public string? VarName { get; }

        public override string ToRegex() {
            if (VarName != null) {
                return $"(?<{VarName}>{Pattern.ToRegex()})";
            } else {
                return $"(?:{Pattern.ToRegex()})";
            }
        }

        public override string ToString() {
            if (VarName != null) {
                return $"{{{{{Pattern}={VarName}}}}}";
            } else {
                return $"{{{{{Pattern}}}}}";
            }
        }
    }
}
