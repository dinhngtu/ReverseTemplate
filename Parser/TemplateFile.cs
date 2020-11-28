// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Linq;

namespace ReverseTemplate.Parser {
    public class TemplateFile {
        public TemplateLine? FileNameTemplateLine { get; }
        public IReadOnlyList<FilterLine> FilterLines { get; }
        public IReadOnlyList<TemplateLine> TemplateLines { get; }
        public TemplateFile(IEnumerable<TemplateLine> lines, IEnumerable<FilterLine> filters, TemplateLine? fileNameLine = null) {
            FileNameTemplateLine = fileNameLine;
            FilterLines = filters.ToList();
            TemplateLines = lines.ToList();
        }
    }
}
