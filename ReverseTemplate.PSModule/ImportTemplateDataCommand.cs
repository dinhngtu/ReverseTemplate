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
        public bool Multiple { get; set; }

        [Parameter()]
        public bool Recurse { get; set; } = true;

        void WriteTemplateLine(CachedTemplateLine line) {
            WriteVerbose(line.RegexString + " <" + string.Join(";", line.AllCaptureGroups) + ">");
        }

        protected override void EndProcessing() {
            var engine = Template.Create(SessionState.Path.GetUnresolvedProviderPathFromPSPath(TemplatePath));
            WriteTemplateLine(engine.FileNameTemplateLine);
            foreach (var line in engine.TemplateLines) {
                WriteTemplateLine(line);
            }
            var rootPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(RootPath);
            WriteVerbose("Processing root path " + rootPath);
            var rootDir = new DirectoryInfo(rootPath);
            foreach (var file in rootDir.EnumerateFiles("*", Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
                var relativeFilePath = file.FullName.Substring(rootDir.FullName.Length);
                WriteVerbose("Processing file " + relativeFilePath);
                using var fileText = file.OpenText();
                foreach (var record in engine.ProcessRecords(fileText, Multiple, relativeFilePath: relativeFilePath)) {
                    var pso = new PSObject();
                    foreach (var kv in record) {
                        pso.Members.Add(new PSNoteProperty(kv.Key, kv.Value));
                    }
                    WriteObject(pso);
                }
            }
        }
    }
}
