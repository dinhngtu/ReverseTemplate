// SPDX-License-Identifier: GPL-3.0-only

using Microsoft.PowerShell.Commands;
using ReverseTemplate.Engine;
using ReverseTemplate.Parser;
using System;
using System.Collections;
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

        void WriteTemplateLine(CachedTemplateLine line) {
            WriteVerbose(line.RegexString + " <" + string.Join(";", line.AllCaptureGroups) + ">");
        }

        void SetProperty(PSObject pso, CaptureResult result) {
            PSObject prop = pso;
            // traverse variable parts to before the final part
            for (int i = 0; i < result.Section.VarPath.Count - 1; i++) {
                if (result.Section.VarPath[i] is ArrayVariablePart) {
                    throw new InvalidOperationException("Array variable part only allowed at the end of the capture");
                }
                var member = pso.Members.Match(result.Section.VarPath[i].Name).SingleOrDefault();
                if (member?.Value is PSObject _prop) {
                    prop = _prop;
                } else {
                    _prop = new PSObject();
                    prop.Members.Add(new PSNoteProperty(result.Section.VarPath[i].Name, _prop));
                    prop = _prop;
                }
            }
            // set final capture part
            if (result.Section.VarPath.Count > 0) {
                var vp = result.Section.VarPath.Last();
                if (vp is ObjectVariablePart) {
                    prop.Members.Add(new PSNoteProperty(vp.Name, result.Value));
                } else if (vp is ArrayVariablePart) {
                    List<PSObject> array;
                    var finalProp = prop.Members.Match(vp.Name).SingleOrDefault();
                    if (finalProp != null) {
                        array = (List<PSObject>)finalProp.Value;
                    } else {
                        array = new List<PSObject>();
                        prop.Members.Add(new PSNoteProperty(vp.Name, array));
                    }
                    array.Add(result.Value);
                }
            }
        }

        protected override void EndProcessing() {
            var engine = Template.Create(SessionState.Path.GetUnresolvedProviderPathFromPSPath(TemplatePath));
            if (engine.FileNameTemplateLine != null) {
                WriteTemplateLine(engine.FileNameTemplateLine);
            }
            foreach (var line in engine.TemplateLines) {
                WriteTemplateLine(line);
            }
            var rootPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(RootPath);
            WriteVerbose("Processing root path " + rootPath);
            var rootDir = new DirectoryInfo(rootPath);
            foreach (var file in rootDir.EnumerateFiles("*", Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
                var relativeFilePath = file.FullName.Substring(rootDir.FullName.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                WriteVerbose("Processing file " + relativeFilePath);
                using var fileText = file.OpenText();
                foreach (var record in engine.ProcessRecords(fileText, Multiple, relativeFilePath: relativeFilePath)) {
                    var pso = new PSObject();
                    foreach (var kv in record) {
                        SetProperty(pso, kv.Value);
                    }
                    WriteObject(pso);
                }
            }
        }
    }
}
