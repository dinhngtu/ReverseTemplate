using Microsoft.PowerShell.Commands;
using ReverseTemplate.Engine;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace ReverseTemplate.PSModule {
    [Cmdlet(VerbsData.Import, "TemplatedData")]
    public class ImportTemplateDataCommand : PSCmdlet {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string[] Path { get; set; }

        [Parameter(Mandatory = true)]
        public string TemplatePath { get; set; }

        public bool Multiple { get; set; }

        protected override void EndProcessing() {
            var engine = Template.Create(SessionState.Path.GetUnresolvedProviderPathFromPSPath(TemplatePath));
            WriteVerbose(engine.FileNameTemplateLine.RegexString);
            foreach (var line in engine.TemplateLines) {
                WriteVerbose(line.RegexString);
                WriteVerbose(string.Join(";", line.AllCaptureGroups));
            }
            foreach (var pattern in Path) {
                foreach (var filename in SessionState.Path.GetResolvedProviderPathFromPSPath(pattern, out var provider)) {
                    if (provider.ImplementingType == typeof(FileSystemProvider)) {
                        WriteVerbose(filename);
                        foreach (var record in engine.ProcessRecords(filename, Multiple)) {
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
    }
}
