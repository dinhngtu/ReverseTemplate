using System;

namespace ReverseTemplate.Parser {
    [Flags]
    public enum CaptureFlags {
        Optional = 1,
        SkipDataLineIfNotFound = 2,
        SkipTemplateLineIfNotFound = 4,
    }
}
