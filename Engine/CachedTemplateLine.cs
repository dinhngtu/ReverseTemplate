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
        public IReadOnlyList<LineSection> Sections { get; }
        public IReadOnlyList<string> CaptureNames { get; }
        public string RegexString { get; }
        public Regex RegexObject { get; }
        public IReadOnlyList<string> AllCaptureGroups { get; }
        public IReadOnlyList<string> ForwardCaptureNames { get; }

        string ToRegex(IEnumerable<LineSection> sections) {
            var sb = new StringBuilder();
            foreach (var s in sections) {
                sb.Append(s.ToRegex());
            }
            return sb.ToString();
        }

        public CachedTemplateLine(TemplateLine tl) {
            TemplateLine = tl;
            Sections = tl.Sections.Select(x => {
                if (x is CaptureSection cs && cs.VarName == null) {
                    if (cs.Flags.HasFlag(CaptureFlags.SkipDataLineIfNotFound) || cs.Flags.HasFlag(CaptureFlags.SkipTemplateLineIfNotFound)) {
                        return new CaptureSection(cs.Pattern, $"__ctl", cs.Flags);
                    }
                }
                return x;
            }).ToList();
            CaptureNames = Sections.OfType<CaptureSection>().Where(cs => cs.VarName != null && !cs.VarName.StartsWith("__")).Select(x => x.VarName!).ToList();
            ForwardCaptureNames = Sections.OfType<CaptureSection>().Where(cs => cs.Flags.HasFlag(CaptureFlags.SkipDataLineIfNotFound)).Select(x => x.VarName!).ToList();
            RegexString = ToRegex(Sections);
            RegexObject = new Regex(RegexString);
            AllCaptureGroups = RegexObject.GetGroupNames().Where(g => !g.StartsWith("__") && !Regex.IsMatch(g, "^[0-9]*$")).ToList();
        }

        public CachedTemplateLine(IEnumerable<LineSection> sections) : this(new TemplateLine(sections)) {
        }
    }
}
