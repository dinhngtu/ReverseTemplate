// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;

namespace ReverseTemplate.Parser {
    [Flags]
    public enum CaptureFlags {
        Optional = 1,
        SkipDataLineIfNotFound = 2,
        SkipTemplateLineIfNotFound = 4,
    }

    public static class CaptureFlagsHelper {
        public static CaptureFlags ParseFlags(IEnumerable<char> flagChars) {
            CaptureFlags flags = 0;
            foreach (var f in flagChars) {
                flags |= f switch
                {
                    '?' => CaptureFlags.Optional,
                    '>' => CaptureFlags.SkipDataLineIfNotFound,
                    '<' => CaptureFlags.SkipTemplateLineIfNotFound,
                    _ => 0,
                };
            }
            return flags;
        }
    }
}
