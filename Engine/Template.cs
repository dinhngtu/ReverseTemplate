// SPDX-License-Identifier: GPL-3.0-only

using ReverseTemplate.Parser;
using Superpower;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReverseTemplate.Engine {
    using Record = Dictionary<string, CaptureResult>;

    public class Template {
        private readonly TemplateFile _templateFile;
        private readonly List<CachedTemplateLine> _templateLines;
        private readonly CachedTemplateLine? _fileNameLine;
        private readonly string? _identifier;

        public TemplateFile TemplateFile => _templateFile;
        public CachedTemplateLine? FileNameTemplateLine => _fileNameLine;
        public IReadOnlyList<CachedTemplateLine> TemplateLines => _templateLines.AsReadOnly();
        public string? Identifier => _identifier;

        public Template(TemplateFile templateFile, string? identifier = null) {
            _templateFile = templateFile;
            _templateLines = _templateFile.TemplateLines.Select(tl => new CachedTemplateLine(tl)).ToList();
            _fileNameLine = _templateFile.FileNameTemplateLine != null ? new CachedTemplateLine(_templateFile.FileNameTemplateLine) : null;
            _identifier = identifier;
        }

        public static Template Create(TextReader data, string? identifier = null) {
            return new Template(TemplateFileParser.TemplateFile.Parse(data.ReadToEnd()), identifier);
        }

        public static Template Create(string templateFileName) {
            using var templateReader = new StreamReader(templateFileName);
            return Create(templateReader, templateFileName);
        }

        public bool IsFileMatch(string fileName) {
            return _fileNameLine?.RegexObject.IsMatch(fileName) ?? true;
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

        IEnumerable<Record> ProcessRecords(
            IEnumerable<(CachedTemplateLine line, int index)> templates,
            TextReader data,
            bool multiple,
            bool useFilter,
            TemplateOptions options) {
            var lineCount = 0;
            do {
                var record = new Record();
                foreach ((var tl, var index) in templates) {
                    while (true) {
                        Match m;
                        do {
                            string? l;
                            lineCount++;
                            do {
                                l = data.ReadLine();
                                if (l == null) {
                                    if (tl.RepeatTemplateLineUntilNotFound) {
                                        yield return record;
                                        yield break;
                                    }
                                    if (index == 0) {
                                        yield break;
                                    } else {
                                        throw new EndOfStreamException($"reached end of data at template index {index + 1}");
                                    }
                                }
                                // skip empty data lines at beginning of template instead of the end
                                // to avoid having to determine which is the last template line
                            } while (options.SkipDataGapLines && index == 0 && (options.WhiteSpaceOnlyLinesAreEmpty ? string.IsNullOrWhiteSpace(l) : string.IsNullOrEmpty(l)));

                            if (useFilter) {
                                foreach (var filter in _templateFile.FilterLines) {
                                    l = Regex.Replace(l, filter.Pattern.ToRegex(), filter.Replacement);
                                }
                            }
                            m = tl.RegexObject.Match(l);
                        } while (tl.ForwardCaptureNames.Any(x => !m.Groups[x].Success));  // SkipDataLineIfNotFound

                        if (!m.Success) {
                            if (tl.SkipTemplateLineIfNotFound) {
                                // to next template line
                                break;
                            } else if (!tl.RepeatTemplateLineUntilNotFound) {
                                throw new ArgumentException($"line {lineCount} doesn't match template index {index + 1}");
                            }
                        }
                        foreach (var group in tl.Captures) {
                            var g = m.Groups[group.VarName!];
                            if (!record.ContainsKey(group.VarName!)) {
                                record[group.VarName!] = new CaptureResult(group);
                            }
                            if (g.Success) {
                                record[group.VarName!].Values.Add(g.Value);
                            }
                        }
                        if (m.Success && tl.RepeatTemplateLineUntilNotFound) {
                            // repeat on this template line
                            continue;
                        }
                        // to next template line
                        break;
                    }
                }
                yield return record;
            } while (multiple);
        }

        public IEnumerable<Record> ProcessRecords(TextReader data, bool multiple, TemplateOptions? options = null, string? relativeFilePath = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            var records = ProcessRecords(GetEffectiveTemplateLines(options), data, multiple, true, options);
            if (FileNameTemplateLine != null) {
                using var nameData = new StringReader(relativeFilePath);
                List<KeyValuePair<string, CaptureResult>> nameRecord;
                try {
                    // don't use filters when processing filename
                    nameRecord = ProcessRecords(GetFileNameTemplate(), nameData, false, false, TemplateOptions.Default).Single().ToList();
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

        async IAsyncEnumerable<Record> ProcessRecordsAsync(
            IEnumerable<(CachedTemplateLine line, int index)> templates,
            TextReader data,
            bool multiple,
            bool useFilter,
            TemplateOptions options) {
            var lineCount = 0;
            do {
                var record = new Record();
                foreach ((var tl, var index) in templates) {
                    while (true) {
                        Match m;
                        do {
                            string? l;
                            lineCount++;
                            do {
                                l = await data.ReadLineAsync();
                                if (l == null) {
                                    if (tl.RepeatTemplateLineUntilNotFound) {
                                        yield return record;
                                        yield break;
                                    }
                                    if (index == 0) {
                                        yield break;
                                    } else {
                                        throw new EndOfStreamException($"reached end of data at template index {index + 1}");
                                    }
                                }
                                // skip empty data lines at beginning of template instead of the end
                                // to avoid having to determine which is the last template line
                            } while (options.SkipDataGapLines && index == 0 && (options.WhiteSpaceOnlyLinesAreEmpty ? string.IsNullOrWhiteSpace(l) : string.IsNullOrEmpty(l)));

                            if (useFilter) {
                                foreach (var filter in _templateFile.FilterLines) {
                                    l = Regex.Replace(l, filter.Pattern.ToRegex(), filter.Replacement);
                                }
                            }
                            m = tl.RegexObject.Match(l);
                        } while (tl.ForwardCaptureNames.Any(x => !m.Groups[x].Success));  // SkipDataLineIfNotFound

                        if (!m.Success) {
                            if (tl.SkipTemplateLineIfNotFound) {
                                // to next template line
                                break;
                            } else if (!tl.RepeatTemplateLineUntilNotFound) {
                                throw new ArgumentException($"line {lineCount} doesn't match template index {index + 1}");
                            }
                        }
                        foreach (var group in tl.Captures) {
                            var g = m.Groups[group.VarName!];
                            if (!record.ContainsKey(group.VarName!)) {
                                record[group.VarName!] = new CaptureResult(group);
                            }
                            if (g.Success) {
                                record[group.VarName!].Values.Add(g.Value);
                            }
                        }
                        if (m.Success && tl.RepeatTemplateLineUntilNotFound) {
                            // repeat on this template line
                            continue;
                        }
                        // to next template line
                        break;
                    }
                }
                yield return record;
            } while (multiple);
        }

        public async IAsyncEnumerable<Record> ProcessRecordsAsync(TextReader data, bool multiple, TemplateOptions? options = null, string? relativeFilePath = null) {
            if (options == null) {
                options = TemplateOptions.Default;
            }
            var records = ProcessRecordsAsync(GetEffectiveTemplateLines(options), data, multiple, true, options);
            if (FileNameTemplateLine != null) {
                using var nameData = new StringReader(relativeFilePath);
                List<KeyValuePair<string, CaptureResult>> nameRecord;
                try {
                    // we know that filename is coming from StringReader anyway
                    // no need to use async
                    nameRecord = ProcessRecords(GetFileNameTemplate(), nameData, false, false, TemplateOptions.Default).Single().ToList();
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
