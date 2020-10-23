using ReverseTemplate.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReverseTemplate.Engine {
    public class Template {
        private readonly List<TemplateLine> _templateLines;
        private readonly List<Regex> _cache;

        public IReadOnlyList<TemplateLine> TemplateLines => _templateLines.AsReadOnly();

        public Template(IEnumerable<TemplateLine> lines) {
            _templateLines = new List<TemplateLine>(lines);
            _cache = _templateLines.Select(l => new Regex(l.ToRegex())).ToList();
        }

        IEnumerable<(TemplateLine line, Regex regex, bool first)> FilterTemplate(TemplateOptions options, bool loop = false) {
            do {
                for (int i = 0; i < _templateLines.Count; i++) {
                    if (options.SkipTrailingTemplateLines && i == _templateLines.Count - 1 && _templateLines[i].Sections.Count == 0) {
                        continue;
                    }
                    yield return (_templateLines[i], _cache[i], i == 0);
                }
            } while (loop);
        }

        public async IAsyncEnumerable<IDictionary<string, string?>> ProcessRecordsAsync(TextReader data, bool multiple, TemplateOptions? options = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            do {
                var dict = new Dictionary<string, string?>();
                foreach ((var _, var r, var first) in FilterTemplate(options)) {
                    string? l;
                    do {
                        l = await data.ReadLineAsync();
                        if (l == null) {
                            if (first) {
                                yield break;
                            } else {
                                throw new EndOfStreamException("reached end of data in the middle of template");
                            }
                        }
                        // skip empty data lines at beginning of template instead of the end
                        // to avoid having to determine which is the last template line
                    } while (options.SkipDataGapLines && first && l == "");

                    Match m = r.Match(l);
                    if (m == null) {
                        throw new Exception("line doesn't match");
                    }
                    foreach (var groupName in r.GetGroupNames()) {
                        var g = m.Groups[groupName];
                        dict[groupName] = g.Success ? g.Value : null;
                    }
                }
                yield return dict;
            } while (multiple);
        }
    }
}
