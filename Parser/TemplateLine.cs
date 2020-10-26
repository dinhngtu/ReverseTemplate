using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseTemplate.Parser {
    public class TemplateLine {
        public IReadOnlyList<LineSection> Sections { get; }

        public TemplateLine(IEnumerable<LineSection> sections) {
            Sections = new List<LineSection>(sections);
        }
    }
}
