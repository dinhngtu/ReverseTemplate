// SPDX-License-Identifier: GPL-3.0-only

namespace ReverseTemplate.Engine {
    public class CaptureResult {
        public CaptureResult(CachedCaptureSection section, string? value) {
            Section = section;
            Value = value;
        }

        public CachedCaptureSection Section { get; }
        public string? Value { get; }
    }
}
