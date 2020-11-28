// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;

namespace ReverseTemplate.Parser {
    public class TemplateLine {
        public IReadOnlyList<LineSection> Sections { get; }

        public TemplateLine(IEnumerable<LineSection> sections) {
            Sections = new List<LineSection>(sections);
        }
    }
}
