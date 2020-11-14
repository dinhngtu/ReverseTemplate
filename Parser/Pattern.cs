// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseTemplate.Parser {
    public abstract class Pattern {
        public abstract string ToRegex();
    }
}
