// SPDX-License-Identifier: GPL-3.0-only

using ReverseTemplate.Parser;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReverseTemplate.Engine {
    public class CachedTemplateLine {
        public TemplateLine TemplateLine { get; }
        public IReadOnlyList<LineSection> Sections { get; }
        public IReadOnlyList<CachedCaptureSection> Captures { get; }
        public string RegexString { get; }
        public Regex RegexObject { get; }
        public IReadOnlyList<string> AllCaptureGroups { get; }
        public bool SkipDataLineIfNotFound { get; }
        public bool SkipTemplateLineIfNotFound { get; }
        public bool RepeatTemplateLineUntilNotFound { get; }

        string ToRegex(IEnumerable<LineSection> sections) {
            var sb = new StringBuilder();
            sb.Append('^');
            foreach (var s in sections) {
                sb.Append(s.ToRegex());
            }
            sb.Append('$');
            return sb.ToString();
        }

        public CachedTemplateLine(TemplateLine tl) {
            TemplateLine = tl;
            Sections = tl.Sections.Select(x => {
                if (x is CaptureSection cs) {
                    // wrap in CachedCaptureSection in any case for the computed varname
                    if (cs.VarPath.Count == 0 && cs.Flags.HasFlag(CaptureFlags.SkipDataLineIfNotFound) || cs.Flags.HasFlag(CaptureFlags.SkipTemplateLineIfNotFound)) {
                        // if current CaptureSection doesn't actually capture
                        // we still need to keep track of its name to know if the current line match failed
                        // fake a capture name in this case
                        return new CachedCaptureSection(new CaptureSection(cs.Pattern, new VariablePart[] { new ObjectVariablePart($"__ctl") }, cs.Flags));
                    } else {
                        return new CachedCaptureSection(cs);
                    }
                }
                return x;
            }).ToList();
            var cachedCaptureSections = Sections.OfType<CachedCaptureSection>().ToList();
            Captures = cachedCaptureSections.Where(cs => cs.VarPath.Any(x => !x.Name.StartsWith("__"))).ToList();
            RegexString = ToRegex(Sections);
            RegexObject = new Regex(RegexString);
            AllCaptureGroups = RegexObject.GetGroupNames().Where(g => !g.StartsWith("__") && !Regex.IsMatch(g, "^[0-9]*$")).ToList();
            SkipDataLineIfNotFound = cachedCaptureSections.Any(x => x.Flags.HasFlag(CaptureFlags.SkipDataLineIfNotFound));
            SkipTemplateLineIfNotFound = cachedCaptureSections.Any(x => x.Flags.HasFlag(CaptureFlags.SkipTemplateLineIfNotFound));
            RepeatTemplateLineUntilNotFound = cachedCaptureSections.Any(x => x.Flags.HasFlag(CaptureFlags.RepeatTemplateLineUntilNotFound));
        }

        public CachedTemplateLine(IEnumerable<LineSection> sections) : this(new TemplateLine(sections)) {
        }
    }
}
