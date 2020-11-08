using Microsoft.PowerShell.Commands;
using ReverseTemplate.Engine;
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

        protected override void EndProcessing() {
            var engine = Template.Create(SessionState.Path.GetUnresolvedProviderPathFromPSPath(TemplatePath));
            WriteVerbose(engine.FileNameTemplateLine.RegexString);
            foreach (var line in engine.TemplateLines) {
                WriteVerbose(line.RegexString);
                WriteVerbose(string.Join(";", line.AllCaptureGroups));
            }
            var rootPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(RootPath);
            WriteVerbose(rootPath);
            var rootDir = new DirectoryInfo(rootPath);
            foreach (var file in rootDir.EnumerateFiles("*", Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
                using var fileText = file.OpenText();
                foreach (var record in engine.ProcessRecords(fileText, Multiple, relativeFilePath: file.FullName.Substring(rootDir.FullName.Length))) {
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
