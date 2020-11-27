// SPDX-License-Identifier: GPL-3.0-only

using ReverseTemplate.Parser;
using Sprache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReverseTemplate.Engine {
    public class Template {
        private readonly TemplateFile _templateFile;
        private readonly List<CachedTemplateLine> _templateLines;
        private readonly CachedTemplateLine? _fileNameLine;

        public CachedTemplateLine? FileNameTemplateLine => _fileNameLine;
        public IReadOnlyList<CachedTemplateLine> TemplateLines => _templateLines.AsReadOnly();

        public Template(TemplateFile templateFile) {
            _templateFile = templateFile;
            _templateLines = _templateFile.TemplateLines.Select(tl => new CachedTemplateLine(tl)).ToList();
            _fileNameLine = _templateFile.FileNameTemplateLine != null ? new CachedTemplateLine(_templateFile.FileNameTemplateLine) : null;
        }

        public static Template Create(TextReader data) {
            return new Template(TemplateFileParser.TemplateFile.Parse(data.ReadToEnd()));
        }

        public static Template Create(string templateFileName) {
            using var templateReader = new StreamReader(templateFileName);
            return Create(templateReader);
        }

        IEnumerable<(CachedTemplateLine line, int index)> GetEffectiveTemplateLines(TemplateOptions options, bool loop = false) {
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
            var lineCount = 0;
            do {
                var dict = new Dictionary<string, string?>();
                foreach ((var tl, var index) in templates) {
                    Match m;
                    do {
                        string? l;
                        lineCount++;
                        do {
                            l = data.ReadLine();
                            if (l == null) {
                                if (index == 0) {
                                    yield break;
                                } else {
                                    throw new EndOfStreamException($"reached end of data at template index {index + 1}");
                                }
                            }
                            // skip empty data lines at beginning of template instead of the end
                            // to avoid having to determine which is the last template line
                        } while (options.SkipDataGapLines && index == 0 && (options.WhiteSpaceOnlyLinesAreEmpty ? string.IsNullOrWhiteSpace(l) : string.IsNullOrEmpty(l)));

                        m = tl.RegexObject.Match(l);
                    } while (tl.ForwardCaptureNames.Any(x => !m.Groups[x].Success));

                    if (!m.Success) {
                        throw new ArgumentException($"line {lineCount} doesn't match template index {index + 1}");
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
            var records = ProcessRecords(GetEffectiveTemplateLines(options), data, multiple, options);
            if (FileNameTemplateLine != null) {
                using var nameData = new StringReader(relativeFilePath);
                List<KeyValuePair<string, string?>> nameRecord;
                try {
                    nameRecord = ProcessRecords(GetFileNameTemplate(), nameData, false, TemplateOptions.Default).Single().ToList();
                } catch {
                    yield break;
                }
                foreach (var record in records) {
                    nameRecord.ForEach(kv => record[kv.Key] = kv.Value);
                    yield return record;
                }
            } else {
                foreach (var record in records) {
                    yield return record;
                }
            }
        }

        async IAsyncEnumerable<IDictionary<string, string?>> ProcessRecordsAsync(IEnumerable<(CachedTemplateLine line, int index)> templates, TextReader data, bool multiple, TemplateOptions options) {
            var lineCount = 0;
            do {
                var dict = new Dictionary<string, string?>();
                foreach ((var tl, var index) in templates) {
                    Match m;
                    do {
                        string? l;
                        do {
                            l = await data.ReadLineAsync();
                            lineCount++;
                            if (l == null) {
                                if (index == 0) {
                                    yield break;
                                } else {
                                    throw new EndOfStreamException($"reached end of data at template index {index}");
                                }
                            }
                            // skip empty data lines at beginning of template instead of the end
                            // to avoid having to determine which is the last template line
                        } while (options.SkipDataGapLines && index == 0 && (options.WhiteSpaceOnlyLinesAreEmpty ? string.IsNullOrWhiteSpace(l) : string.IsNullOrEmpty(l)));

                        m = tl.RegexObject.Match(l);
                    } while (tl.ForwardCaptureNames.Any(x => !m.Groups[x].Success));

                    if (!m.Success) {
                        throw new ArgumentException($"line {lineCount} doesn't match template index {index + 1}");
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
            var records = ProcessRecordsAsync(GetEffectiveTemplateLines(options), data, multiple, options);
            if (FileNameTemplateLine != null) {
                using var nameData = new StringReader(relativeFilePath);
                List<KeyValuePair<string, string?>> nameRecord;
                try {
                    nameRecord = ProcessRecords(GetFileNameTemplate(), nameData, false, TemplateOptions.Default).Single().ToList();
                } catch {
                    yield break;
                }
                await foreach (var record in records) {
                    nameRecord.ForEach(kv => record[kv.Key] = kv.Value);
                    yield return record;
                }
            } else {
                await foreach (var record in records) {
                    yield return record;
                }
            }
        }
    }
}
