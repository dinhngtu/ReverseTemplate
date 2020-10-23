using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseTemplate.Parser {
    public class CaptureSection : LineSection {
        public CaptureSection(Pattern pattern, string varName) {
            Pattern = pattern;
            VarName = varName;
        }

        public Pattern Pattern { get; private set; }
        public string VarName { get; private set; }

        public override string ToRegex() => $"(?<{VarName}>{Pattern.ToRegex()})";

        public override string ToString() => $"{{{{{Pattern}={VarName}}}}}";
    }
}
