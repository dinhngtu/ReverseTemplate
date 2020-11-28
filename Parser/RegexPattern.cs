// SPDX-License-Identifier: GPL-3.0-only

namespace ReverseTemplate.Parser {
    public class RegexPattern : Pattern {
        public RegexPattern(string regex) {
            Regex = regex;
        }

        public string Regex { get; }

        public override string ToRegex() => Regex;

        public override string ToString() => $"/{Regex}/";
    }
}
