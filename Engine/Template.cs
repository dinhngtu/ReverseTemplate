using ReverseTemplate.Parser;
using Superpower;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReverseTemplate.Engine {
    public class Template {
        private List<CachedTemplateLine> _templateLines;

        public CachedTemplateLine? FileNameTemplateLine { get; private set; }
        public IReadOnlyList<CachedTemplateLine> TemplateLines => _templateLines.AsReadOnly();

        static IEnumerable<CachedTemplateLine> ParseLines(TextReader reader) {
            string? l;
            var lineNum = 0;
            while ((l = reader.ReadLine()) != null) {
                // output lineNum is 1-indexed
                lineNum++;
                if (TemplateParser.TryParse(l, out var tl, out var error, out var pos)) {
                    yield return new CachedTemplateLine(tl);
                } else {
                    var realPos = new Superpower.Model.Position(pos.Absolute, lineNum, pos.Column);
                    throw new ParseException(error, realPos);
                }
            }
        }

        public Template(IEnumerable<CachedTemplateLine> lines, CachedTemplateLine? fileNameTemplateLine = null) {
            _templateLines = new List<CachedTemplateLine>(lines);
            FileNameTemplateLine = fileNameTemplateLine;
        }

        public Template(
            IEnumerable<TemplateLine> lines,
            TemplateLine? fileNameTemplateLine = null) : this(
                lines.Select(tl => new CachedTemplateLine(tl)),
                fileNameTemplateLine != null ? new CachedTemplateLine(fileNameTemplateLine) : null) {
        }

        public static Template Create(TextReader data) {
            return new Template(ParseLines(data));
        }

        public static Template Create(string templateFileName) {
            using var templateReader = new StreamReader(templateFileName);
            TemplateLine? fntl = null;
            if (templateReader.Peek() == '#') {
                if (!TemplateParser.TryParse(templateReader.ReadLine().Substring(1), out fntl, out var err, out var pos)) {
                    throw new ParseException(err, pos);
                }
            }
            return new Template(ParseLines(templateReader), fntl != null ? new CachedTemplateLine(fntl) : null);
        }

        IEnumerable<(CachedTemplateLine line, int index)> FilterTemplate(TemplateOptions options, bool loop = false) {
            do {
                for (int i = 0; i < _templateLines.Count; i++) {
                    if (options.SkipTrailingTemplateLines && i == _templateLines.Count - 1 && _templateLines[i].Sections.Count == 0) {
                        continue;
                    }
                    yield return (_templateLines[i], i);
                }
            } while (loop);
        }

        IEnumerable<(CachedTemplateLine line, int index)> GetFileNameTemplate() {
            if (FileNameTemplateLine == null) {
                throw new InvalidOperationException("no file name template");
            }
            yield return (FileNameTemplateLine, 0);
        }

        IEnumerable<IDictionary<string, string?>> ProcessRecords(IEnumerable<(CachedTemplateLine line, int index)> templates, TextReader data, bool multiple, TemplateOptions options) {
            do {
                var dict = new Dictionary<string, string?>();
                foreach ((var tl, var index) in templates) {
                    string? l;
                    do {
                        l = data.ReadLine();
                        if (l == null) {
                            if (index == 0) {
                                yield break;
                            } else {
                                throw new EndOfStreamException($"reached end of data at template index {index}");
                            }
                        }
                        // skip empty data lines at beginning of template instead of the end
                        // to avoid having to determine which is the last template line
                    } while (options.SkipDataGapLines && index == 0 && string.IsNullOrEmpty(l));

                    Match m = tl.RegexObject.Match(l);
                    var forwardSections = tl.TemplateLine.Sections.OfType<CaptureSection>().Where(x => x.Flags.HasFlag(CaptureFlags.SkipDataLineIfNotFound));
                    if (forwardSections.Any(x => !m.Groups[x.VarName].Success)) {
                        continue;
                    }
                    if (m == null) {
                        throw new Exception("line doesn't match");
                    }
                    foreach (var groupName in options.UseAllGroupNames ? tl.AllCaptureGroups : tl.CaptureNames) {
                        var g = m.Groups[groupName];
                        dict[groupName] = g.Success ? g.Value : null;
                    }
                }
                yield return dict;
            } while (multiple);
        }

        public IEnumerable<IDictionary<string, string?>> ProcessRecords(TextReader data, bool multiple, TemplateOptions? options = null, string? relativeFilePath = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            var dict = ProcessRecords(FilterTemplate(options), data, multiple, options);
            if (FileNameTemplateLine != null) {
                using var nameData = new StringReader(relativeFilePath);
                var nameRecord = ProcessRecords(GetFileNameTemplate(), nameData, false, TemplateOptions.Default).SingleOrDefault()?.ToList();
                if (nameRecord == null) {
                    yield break;
                }
                foreach (var record in dict) {
                    nameRecord.ForEach(kv => record[kv.Key] = kv.Value);
                    yield return record;
                }
            } else {
                foreach (var record in dict) {
                    yield return record;
                }
            }
        }

        async IAsyncEnumerable<IDictionary<string, string?>> ProcessRecordsAsync(IEnumerable<(CachedTemplateLine line, int index)> templates, TextReader data, bool multiple, TemplateOptions options) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            do {
                var dict = new Dictionary<string, string?>();
                foreach ((var tl, var index) in templates) {
                    string? l;
                    do {
                        l = await data.ReadLineAsync();
                        if (l == null) {
                            if (index == 0) {
                                yield break;
                            } else {
                                throw new EndOfStreamException($"reached end of data at template index {index}");
                            }
                        }
                        // skip empty data lines at beginning of template instead of the end
                        // to avoid having to determine which is the last template line
                    } while (options.SkipDataGapLines && index == 0 && string.IsNullOrEmpty(l));

                    Match m = tl.RegexObject.Match(l);
                    var forwardSections = tl.TemplateLine.Sections.OfType<CaptureSection>().Where(x => x.Flags.HasFlag(CaptureFlags.SkipDataLineIfNotFound));
                    if (forwardSections.Any(x => !m.Groups[x.VarName].Success)) {
                        continue;
                    }
                    if (m == null) {
                        throw new Exception("line doesn't match");
                    }
                    foreach (var groupName in options.UseAllGroupNames ? tl.AllCaptureGroups : tl.CaptureNames) {
                        var g = m.Groups[groupName];
                        dict[groupName] = g.Success ? g.Value : null;
                    }
                }
                yield return dict;
            } while (multiple);
        }

        public async IAsyncEnumerable<IDictionary<string, string?>> ProcessRecordsAsync(TextReader data, bool multiple, TemplateOptions? options = null, string? relativeFilePath = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            var dict = ProcessRecordsAsync(FilterTemplate(options), data, multiple, options);
            if (FileNameTemplateLine != null) {
                using var nameData = new StringReader(relativeFilePath);
                IDictionary<string, string?>? nameDict = null;
                await foreach (var _nameDict in ProcessRecordsAsync(GetFileNameTemplate(), nameData, false, TemplateOptions.Default)) {
                    nameDict = _nameDict;
                    break;
                }
                if (nameDict == null) {
                    yield break;
                }
                var nameRecord = nameDict.ToList();
                await foreach (var record in dict) {
                    nameRecord.ForEach(kv => record[kv.Key] = kv.Value);
                    yield return record;
                }
            } else {
                await foreach (var record in dict) {
                    yield return record;
                }
            }
        }
    }
}
