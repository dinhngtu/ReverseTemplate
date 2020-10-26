using ReverseTemplate.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReverseTemplate.Engine {
    public class CachedTemplateLine {
        public TemplateLine TemplateLine { get; }
        public IReadOnlyList<LineSection> Sections => TemplateLine.Sections;
        public IReadOnlyList<string> CaptureNames { get; }
        public string RegexString { get; }
        public Regex RegexObject { get; }
        public IReadOnlyList<string> AllCaptureGroups { get; }

        string ToRegex(IEnumerable<LineSection> sections) {
            var sb = new StringBuilder();
            foreach (var s in sections) {
                sb.Append(s.ToRegex());
            }
            return sb.ToString();
        }

        public CachedTemplateLine(TemplateLine tl) {
            TemplateLine = tl;
            CaptureNames = Sections.OfType<CaptureSection>().Where(x => x.VarName != null).Select(x => x.VarName!).ToList();
            RegexString = ToRegex(Sections);
            RegexObject = new Regex(RegexString);
            AllCaptureGroups = RegexObject.GetGroupNames().Where(x => Regex.IsMatch(x, "^[^0-9]*$")).ToList();
        }

        public CachedTemplateLine(IEnumerable<LineSection> sections) : this(new TemplateLine(sections)) {
        }
    }
}
