// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseTemplate.Parser {
    public class TemplateFile {
        public TemplateLine? FileNameTemplateLine { get; }
        public IReadOnlyList<TemplateLine> TemplateLines { get; }
        public TemplateFile(IEnumerable<TemplateLine> lines, TemplateLine? fileNameLine = null) {
            FileNameTemplateLine = fileNameLine;
            TemplateLines = lines.ToList();
        }
    }
}
