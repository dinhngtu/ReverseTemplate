// SPDX-License-Identifier: GPL-3.0-only

using System.Text.RegularExpressions;

namespace ReverseTemplate.Parser {
    public class TextSection : LineSection {
        public TextSection(string text) => Text = text;

        public string Text { get; }

        public override string ToRegex() => $"(?:{Regex.Escape(Text)})";

        public override string ToString() => Text;
    }
}
