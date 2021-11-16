// SPDX-License-Identifier: GPL-3.0-only

using ReverseTemplate.Engine;
using ReverseTemplate.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace ReverseTemplate.PSModule {
    [Cmdlet(VerbsData.Import, "TemplatedData")]
    public class ImportTemplateDataCommand : PSCmdlet {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string RootPath { get; set; }

        [Parameter(Mandatory = true)]
        public string[] TemplatePath { get; set; }

        [Parameter()]
        public SwitchParameter Multiple { get; set; }

        [Parameter()]
        public SwitchParameter Recurse { get; set; }

        [Parameter()]
        public SwitchParameter AddFilePath { get; set; }

        void WriteTemplateLine(CachedTemplateLine line, string type = null) {
            if (type != null) {
                type += ": ";
            }
            WriteVerbose($"{type}{line.RegexString} <{string.Join("; ", line.AllCaptureGroups)}>");
        }

        void WriteFilterLine(FilterLine filter) {
            WriteVerbose($"filter: {filter.Pattern.ToRegex()} -> {filter.Replacement}");
        }

        T UpsertProperty<T>(PSObject pso, string name) where T : new() {
            T result;
            var member = pso.Members.Match(name).SingleOrDefault();
            if (member != null && member.Value is T t) {
                result = t;
            } else if (member == null) {
                result = new T();
                pso.Members.Add(new PSNoteProperty(name, result));
            } else {
                throw new Exception($"Unexpected property type {member.GetType()}");
            }
            return result;
        }

        void SetProperty(PSObject pso, CaptureResult result) {
            PSObject prop = pso;
            var section = result.Section;
            // traverse variable parts to before the final part
            for (int i = 0; i < section.VarPath.Count - 1; i++) {
                if (section.VarPath[i] is ArrayVariablePart) {
                    throw new InvalidOperationException("Array variable part only allowed at the end of the capture");
                }
                prop = UpsertProperty<PSObject>(prop, section.VarPath[i].Name);
            }
            // set final capture part
            if (section.VarPath.Count > 0) {
                var vp = section.VarPath.Last();
                if (vp is ObjectVariablePart) {
                    prop.Members.Add(new PSNoteProperty(vp.Name, result.Values.Single()));
                } else if (vp is ArrayVariablePart) {
                    var array = UpsertProperty<List<string>>(prop, vp.Name);
                    array.AddRange(result.Values);
                }
            }
        }

        protected override void EndProcessing() {
            var engines = (
                from tp in TemplatePath
                from tpr in SessionState.Path.GetResolvedProviderPathFromPSPath(tp, out var provider)
                select Template.Create(tpr)).ToList();
            foreach (var engine in engines) {
                if (engine.FileNameTemplateLine != null) {
                    WriteTemplateLine(engine.FileNameTemplateLine, "filename");
                }
                foreach (var filter in engine.TemplateFile.FilterLines) {
                    WriteFilterLine(filter);
                }
                foreach (var line in engine.TemplateLines) {
                    WriteTemplateLine(line);
                }
            }
            var rootPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(RootPath);
            WriteVerbose($"Processing root path {rootPath}");
            var rootDir = new DirectoryInfo(rootPath);

            int fileCount = 0, matchCount = 0;
            foreach (var file in rootDir.EnumerateFiles("*", Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
                fileCount++;
                // even if rootDir.FullName contains the / then TrimStart will take care of it
                var relativeFilePath = file.FullName.Substring(rootDir.FullName.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normalizedFilePath = relativeFilePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
                using var fileText = file.OpenText();
                try {
                    var engine = engines.FirstOrDefault(e => e.IsFileMatch(normalizedFilePath));
                    if (engine == null) {
                        continue;
                    }
                    WriteVerbose($"Found template {engine.Identifier} for {normalizedFilePath}");
                    var totalRecords = 0;
                    foreach (var record in engine.ProcessRecords(fileText, Multiple, relativeFilePath: normalizedFilePath)) {
                        var pso = new PSObject();
                        if (engines.Count > 1 && engine.Identifier != null) {
                            pso.Members.Add(new PSNoteProperty("_template", engine.Identifier));
                        }
                        if (AddFilePath) {
                            pso.Members.Add(new PSNoteProperty("_file", normalizedFilePath));
                        }
                        foreach (var kv in record) {
                            SetProperty(pso, kv.Value);
                        }
                        WriteObject(pso);
                        totalRecords++;
                    }
                    WriteVerbose($"Emitted {totalRecords} records");
                } catch (Exception ex) {
                    WriteError(new ErrorRecord(ex, "FileError", ErrorCategory.InvalidData, normalizedFilePath));
                    continue;
                }

                matchCount++;
                if (fileCount % 100 == 0) {
                    WriteProgress(new ProgressRecord(0, "Importing files...", $"Matched {matchCount} files of {fileCount}.") {
                        PercentComplete = -1,
                        SecondsRemaining = -1,
                    });
                }
            }
            WriteProgress(new ProgressRecord(0, "Importing files...", $"Matched {matchCount} files of {fileCount}.") {
                RecordType = ProgressRecordType.Completed
            });
        }
    }
}
