using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseTemplate.Parser {
    public class TemplateLine {
        public List<LineSection> Sections { get; }

        public TemplateLine() => Sections = new List<LineSection>();
        public TemplateLine(IEnumerable<LineSection> sections) {
            Sections = new List<LineSection>(sections);
        }

        public string ToRegex() {
            var sb = new StringBuilder();
            foreach (var s in Sections) {
                sb.Append('(');
                sb.Append(s.ToRegex());
                sb.Append(')');
            }
            return sb.ToString();
        }
    }
}
