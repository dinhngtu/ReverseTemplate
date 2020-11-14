// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseTemplate.Parser {
    public class CaptureSection : LineSection {
        public CaptureSection(Pattern pattern, string? varName, CaptureFlags flags) {
            Pattern = pattern;
            VarName = varName;
            Flags = flags;
        }

        public CaptureSection(Pattern pattern, string? varName, IEnumerable<char> flags)
            : this(pattern, varName, CaptureFlagsHelper.ParseFlags(flags)) {
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
