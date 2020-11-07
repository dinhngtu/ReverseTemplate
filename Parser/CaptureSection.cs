using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseTemplate.Parser {
    public class CaptureSection : LineSection {
        public CaptureSection(Pattern pattern, string? varName, IEnumerable<char> flags) {
            Pattern = pattern;
            VarName = varName;
            Flags = 0;
            foreach (var f in flags) {
                Flags |= f switch
                {
                    '?' => CaptureFlags.Optional,
                    _ => 0,
                };
            }
        }

        public Pattern Pattern { get; }
        public string? VarName { get; }
        public CaptureFlags Flags { get; }

        public override string ToRegex() {
            string captureRegex;
            if (VarName != null) {
                captureRegex = $"(?<{VarName}>{Pattern.ToRegex()})";
            } else {
                captureRegex = $"(?:{Pattern.ToRegex()})";
            }
            if (Flags.HasFlag(CaptureFlags.Optional)) {
                captureRegex += "?";
            }
            return captureRegex;
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
