using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseTemplate.Parser {
    public class TemplateLine {
        private readonly List<LineSection> _sections;
        private readonly string _regex;
        private readonly List<string> _captureNames;

        public IReadOnlyList<LineSection> Sections => _sections.AsReadOnly();
        public IReadOnlyList<string> CaptureNames => _captureNames.AsReadOnly();

        public TemplateLine(IEnumerable<LineSection> sections) {
            _sections = new List<LineSection>(sections);
            _regex = ToRegex(_sections);
            _captureNames = _sections.OfType<CaptureSection>().Select(x => x.VarName).ToList();
        }

        string ToRegex(IEnumerable<LineSection> sections) {
            var sb = new StringBuilder();
            foreach (var s in sections) {
                sb.Append('(');
                sb.Append(s.ToRegex());
                sb.Append(')');
            }
            return sb.ToString();
        }

        public string ToRegex() => _regex;
    }
}
