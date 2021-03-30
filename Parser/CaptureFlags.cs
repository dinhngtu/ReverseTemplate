// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReverseTemplate.Parser {
    [Flags]
    public enum CaptureFlags {
        Optional = 1,
        SkipDataLineIfNotFound = 2,
        SkipTemplateLineIfNotFound = 4,
        RepeatTemplateLineUntilNotFound = 8,
    }

    public static class CaptureFlagsHelper {
        static void TestFlagConflict(CaptureFlags f, params CaptureFlags[] conflicting) {
            var matching = conflicting.Where(x => f.HasFlag(x)).ToArray();
            if (matching.Length > 1) {
                throw new ArgumentException($"flags {string.Join(", ", matching.Select(x => x.ToString()))} are not compatible");
            }
        }

        public static CaptureFlags ParseFlags(IEnumerable<char> flagChars) {
            CaptureFlags flags = 0;
            foreach (var f in flagChars) {
                flags |= f switch {
                    '?' => CaptureFlags.Optional,
                    '>' => CaptureFlags.SkipDataLineIfNotFound,
                    '<' => CaptureFlags.SkipTemplateLineIfNotFound,
                    '|' => CaptureFlags.RepeatTemplateLineUntilNotFound,
                    _ => 0,
                };
            }
            TestFlagConflict(flags,
                CaptureFlags.SkipDataLineIfNotFound,
                CaptureFlags.SkipTemplateLineIfNotFound,
                CaptureFlags.RepeatTemplateLineUntilNotFound);
            return flags;
        }
    }
}
