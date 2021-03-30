// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;

namespace ReverseTemplate.Engine {
    public class CaptureResult {
        public CaptureResult(CachedCaptureSection section) {
            Section = section;
            Values = new List<string>();
        }

        public CachedCaptureSection Section { get; }
        public List<string> Values { get; }
    }
}
