// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace ReverseTemplate.Engine {
    public class TemplateOptions {
        public static TemplateOptions Default { get; } = new TemplateOptions() {
            SkipTrailingTemplateLines = true,
            SkipDataGapLines = true,
            WhiteSpaceOnlyLinesAreEmpty = true,
        };

        public bool SkipTrailingTemplateLines { get; set; }

        /// <summary>
        /// Only applies in multi-record mode.
        /// Requires the first template to be non-empty.
        /// </summary>
        public bool SkipDataGapLines { get; set; }

        [Obsolete]
        public bool UseAllGroupNames { get; set; }

        public bool WhiteSpaceOnlyLinesAreEmpty { get; set; }
    }
}
