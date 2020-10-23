using System;
using System.Collections.Generic;
using System.Text;

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
