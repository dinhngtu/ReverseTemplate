using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReverseTemplate.Parser {
    public class TemplateLine {
        public IReadOnlyList<LineSection> Sections { get; }
        public IReadOnlyList<string> CaptureNames { get; }
        public string RegexString { get; }
        public Regex RegexObject { get; }
        public IReadOnlyList<string> AllCaptureGroups { get; }

        public TemplateLine(IEnumerable<LineSection> sections) {
            Sections = new List<LineSection>(sections);
            CaptureNames = Sections.OfType<CaptureSection>().Where(x => x.VarName != null).Select(x => x.VarName!).ToList();
            RegexString = ToRegex(Sections);
            RegexObject = new Regex(RegexString);
            AllCaptureGroups = RegexObject.GetGroupNames().Where(x => Regex.IsMatch(x, "^[^0-9]*$")).ToList();
        }

        string ToRegex(IEnumerable<LineSection> sections) {
            var sb = new StringBuilder();
            foreach (var s in sections) {
                sb.Append(s.ToRegex());
            }
            return sb.ToString();
        }
    }
}
