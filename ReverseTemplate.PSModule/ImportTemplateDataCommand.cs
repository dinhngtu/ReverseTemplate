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
        public string TemplatePath { get; set; }

        [Parameter()]
        public SwitchParameter Multiple { get; set; }

        [Parameter()]
        public SwitchParameter Recurse { get; set; }

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
            } else {
                result = new T();
                pso.Members.Add(new PSNoteProperty(name, result));
            }
            return result;
        }

        void SetProperty(PSObject pso, CaptureResult result) {
            PSObject prop = pso;
            // traverse variable parts to before the final part
            for (int i = 0; i < result.Section.VarPath.Count - 1; i++) {
                if (result.Section.VarPath[i] is ArrayVariablePart) {
                    throw new InvalidOperationException("Array variable part only allowed at the end of the capture");
                }
                prop = UpsertProperty<PSObject>(prop, result.Section.VarPath[i].Name);
            }
            // set final capture part
            if (result.Section.VarPath.Count > 0) {
                var vp = result.Section.VarPath.Last();
                if (vp is ObjectVariablePart) {
                    prop.Members.Add(new PSNoteProperty(vp.Name, result.Value));
                } else if (vp is ArrayVariablePart) {
                    var array = UpsertProperty<List<PSObject>>(prop, vp.Name);
                    array.Add(result.Value);
                }
            }
        }

        protected override void EndProcessing() {
            var engine = Template.Create(SessionState.Path.GetUnresolvedProviderPathFromPSPath(TemplatePath));
            if (engine.FileNameTemplateLine != null) {
                WriteTemplateLine(engine.FileNameTemplateLine, "filename");
            }
            foreach (var filter in engine.TemplateFile.FilterLines) {
                WriteFilterLine(filter);
            }
            foreach (var line in engine.TemplateLines) {
                WriteTemplateLine(line);
            }
            var rootPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(RootPath);
            WriteVerbose($"Processing root path {rootPath}");
            var rootDir = new DirectoryInfo(rootPath);

            int fileCount = 0;
            foreach (var file in rootDir.EnumerateFiles("*", Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
                // even if rootDir.FullName contains the / then TrimStart will take care of it
                var relativeFilePath = file.FullName.Substring(rootDir.FullName.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normalizedFilePath = relativeFilePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
                WriteVerbose("Processing file " + normalizedFilePath);
                using var fileText = file.OpenText();
                try {
                    var totalRecords = 0;
                    foreach (var record in engine.ProcessRecords(fileText, Multiple, relativeFilePath: normalizedFilePath)) {
                        var pso = new PSObject();
                        foreach (var kv in record) {
                            SetProperty(pso, kv.Value);
                        }
                        WriteObject(pso);
                        totalRecords++;
                    }
                    WriteVerbose("Emitted " + totalRecords.ToString() + " records");
                } catch (Exception ex) {
                    throw new Exception($"error at file '{normalizedFilePath}'", ex);
                }

                if (++fileCount % 100 == 0) {
                    WriteProgress(new ProgressRecord(0, "Importing files...", $"{fileCount} files processed.") {
                        PercentComplete = -1,
                        SecondsRemaining = -1,
                    });
                }
            }
            WriteProgress(new ProgressRecord(0, "Importing files...", $"{fileCount} files processed.") {
                RecordType = ProgressRecordType.Completed
            });
        }
    }
}
