using ReverseTemplate.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseTemplate.Engine {
    public class CachedCaptureSection : LineSection {
        public CachedCaptureSection(CaptureSection section) {
            Section = section;
            ComputedVarName = GetComputedVarName(VarPath);
        }

        public CaptureSection Section { get; }
        public string? ComputedVarName { get; }
        public Pattern Pattern => Section.Pattern;
        public IReadOnlyList<VariablePart> VarPath => Section.VarPath;
        public CaptureFlags Flags => Section.Flags;

        static string? GetComputedVarName(IReadOnlyList<VariablePart> varPath) {
            if (varPath.Count == 0) {
                return null;
            }
            return string.Join("__", varPath.Select(x => x.Name));
        }

        public override string ToRegex() {
            string captureRegex;
            if (VarPath.Count != 0) {
                captureRegex = $"(?<{ComputedVarName}>{Pattern.ToRegex()})";
            } else {
                captureRegex = $"(?:{Pattern.ToRegex()})";
            }
            if (Flags.HasFlag(CaptureFlags.Optional)) {
                captureRegex += "?";
            }
            return captureRegex;
        }

        public override string ToString() {
            return $"{{{{{Pattern}}}}}";
        }
    }
}
