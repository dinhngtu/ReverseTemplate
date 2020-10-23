using ReverseTemplate.Engine;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace ReverseTemplate.PSModule {
    [Cmdlet(VerbsData.Import, "TemplatedData")]
    public class ImportTemplateDataCommand : Cmdlet {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string[] Path { get; set; }

        [Parameter(Mandatory = true)]
        public string TemplatePath { get; set; }

        public bool Multiple { get; set; }

        protected override void EndProcessing() {
            using var templateFile = File.OpenText(TemplatePath);
            var engine = new Template(templateFile);
            foreach (var filename in Path) {
                using var file = File.OpenText(filename);
                foreach (var record in engine.ProcessRecords(file, Multiple)) {
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
