// SPDX-License-Identifier: GPL-3.0-only

namespace ReverseTemplate.Parser {
    public class FilterLine {
        public Pattern Pattern { get; }
        public string Replacement { get; }

        public FilterLine(Pattern pattern, string replace) {
            Pattern = pattern;
            Replacement = replace;
        }
    }
}
