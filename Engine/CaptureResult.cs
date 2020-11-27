using ReverseTemplate.Parser;
using System;
using System.Collections.Generic;
using System.Text;

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
