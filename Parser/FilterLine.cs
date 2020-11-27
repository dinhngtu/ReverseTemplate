namespace ReverseTemplate.Parser {
    public class FilterLine {
        public Pattern Pattern { get; }
        public string Replace { get; }

        public FilterLine(Pattern pattern, string replace) {
            Pattern = pattern;
            Replace = replace;
        }
    }
}