namespace ReverseTemplate.Engine {
    public class TemplateOptions {
        public static TemplateOptions Default { get; } = new TemplateOptions() {
            SkipTrailingTemplateLines = true,
            SkipDataGapLines = true,
        };

        public bool SkipTrailingTemplateLines { get; set; }

        /// <summary>
        /// Only applies in multi-record mode.
        /// Requires the first template to be non-empty.
        /// </summary>
        public bool SkipDataGapLines { get; set; }

        public bool CaptureIgnoresSurroundingSpaces { get; set; }
    }
}