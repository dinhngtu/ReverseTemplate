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
        private List<TemplateLine> _templateLines;

        public TemplateLine? FileNameTemplateLine { get; private set; }
        public IReadOnlyList<TemplateLine> TemplateLines => _templateLines.AsReadOnly();

        static IEnumerable<TemplateLine> ParseLines(TextReader reader) {
            string? l;
            var lineNum = 0;
            while ((l = reader.ReadLine()) != null) {
                // output lineNum is 1-indexed
                lineNum++;
                if (TemplateParser.TryParse(l, out var tl, out var error, out var pos)) {
                    yield return tl;
                } else {
                    var realPos = new Superpower.Model.Position(pos.Absolute, lineNum, pos.Column);
                    throw new ParseException(error, realPos);
                }
            }
        }

        public Template(IEnumerable<TemplateLine> lines, TemplateLine? fileNameTemplateLine = null) {
            _templateLines = new List<TemplateLine>(lines);
            FileNameTemplateLine = fileNameTemplateLine;
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
            return new Template(ParseLines(templateReader), fntl);
        }

        IEnumerable<(TemplateLine line, int index)> FilterTemplate(TemplateOptions options, bool loop = false) {
            do {
                for (int i = 0; i < _templateLines.Count; i++) {
                    if (options.SkipTrailingTemplateLines && i == _templateLines.Count - 1 && _templateLines[i].Sections.Count == 0) {
                        continue;
                    }
                    yield return (_templateLines[i], i);
                }
            } while (loop);
        }

        IEnumerable<(TemplateLine line, int index)> GetFileNameTemplate() {
            if (FileNameTemplateLine == null) {
                throw new InvalidOperationException("no file name template");
            }
            yield return (FileNameTemplateLine, 0);
        }

        IEnumerable<IDictionary<string, string?>> ProcessRecords(IEnumerable<(TemplateLine line, int index)> templates, TextReader data, bool multiple, TemplateOptions options) {
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
                    } while (options.SkipDataGapLines && index == 0 && l == "");

                    Match m = tl.RegexObject.Match(l);
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

        public IEnumerable<IDictionary<string, string?>> ProcessRecords(TextReader data, bool multiple, TemplateOptions? options = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            return ProcessRecords(FilterTemplate(options), data, multiple, options);
        }

        public IEnumerable<IDictionary<string, string?>> ProcessRecords(string filename, bool multiple, TemplateOptions? options = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            using var reader = new StreamReader(filename);
            var dict = ProcessRecords(reader, multiple, options);
            if (FileNameTemplateLine != null) {
                var nameData = new StringReader(Path.GetFileName(filename));
                var nameRecord = ProcessRecords(GetFileNameTemplate(), nameData, false, TemplateOptions.Default).Single().ToList();
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

        async IAsyncEnumerable<IDictionary<string, string?>> ProcessRecordsAsync(IEnumerable<(TemplateLine line, int index)> templates, TextReader data, bool multiple, TemplateOptions options) {
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
                    } while (options.SkipDataGapLines && index == 0 && l == "");

                    Match m = tl.RegexObject.Match(l);
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

        public IAsyncEnumerable<IDictionary<string, string?>> ProcessRecordsAsync(TextReader data, bool multiple, TemplateOptions? options = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            return ProcessRecordsAsync(FilterTemplate(options), data, multiple, options);
        }

        public async IAsyncEnumerable<IDictionary<string, string?>> ProcessRecordsAsync(string filename, bool multiple, TemplateOptions? options = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            using var reader = new StreamReader(filename);
            var dict = ProcessRecordsAsync(reader, multiple, options);
            if (FileNameTemplateLine != null) {
                var nameData = new StringReader(Path.GetFileName(filename));
                var nameDict = ProcessRecordsAsync(GetFileNameTemplate(), nameData, false, TemplateOptions.Default);
                await foreach (var record in dict) {
                    await foreach (var nameRecord in nameDict) {
                        foreach (var kv in nameRecord) {
                            record[kv.Key] = kv.Value;
                            yield return record;
                        }
                    }
                }
            } else {
                await foreach (var d in dict) {
                    yield return d;
                }
            }
        }
    }
}
