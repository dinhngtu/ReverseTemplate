// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReverseTemplate.Parser {
    public class CaptureSection : LineSection {
        public CaptureSection(Pattern pattern, IEnumerable<VariablePart> varPath, CaptureFlags flags) {
            Pattern = pattern;
            VarPath = varPath.ToList();
            Flags = flags;
        }

        public CaptureSection(Pattern pattern, IEnumerable<VariablePart> varPath, IEnumerable<char> flags)
            : this(pattern, varPath, CaptureFlagsHelper.ParseFlags(flags)) {
        }

        public Pattern Pattern { get; }
        public IReadOnlyList<VariablePart> VarPath { get; }
        public CaptureFlags Flags { get; }

        public override string ToRegex() {
            throw new NotImplementedException();
            /*
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
            */
        }

        public override string ToString() {
            return $"{{{{{Pattern}}}}}";
            /*
            if (VarPath.Count != 0) {
                return $"{{{{{Pattern}={ComputedVarName}}}}}";
            } else {
                return $"{{{{{Pattern}}}}}";
            }
            */
        }
    }
}
