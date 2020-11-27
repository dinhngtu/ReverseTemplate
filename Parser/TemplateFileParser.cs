using System;
using System.Collections.Generic;
using System.Text;
using Sprache;

namespace ReverseTemplate.Parser {
    public class TemplateFileParser {
        static readonly Parser<string> varName = Parse.LetterOrDigit.Or(Parse.Char('_')).AtLeastOnce().Text();
        static readonly Parser<char> escape = Parse.Char('\\');
        static readonly Parser<char> escapable = Parse.Chars('\\', '/');
        static readonly Parser<char> notEscapable = Parse.CharExcept("\\/");

        static readonly Parser<char> regexChars = notEscapable.XOr(escape.Then(_ => escapable)).Or(escape);

        static readonly Parser<Pattern> _regexPart = regexChars.Many().Text().Select(regex => new RegexPattern(regex));
        static readonly Parser<Pattern> regexPart = _regexPart.Contained(Parse.Char('/'), Parse.Char('/'));

        static readonly Parser<Pattern> _formatPart = Parse.AnyChar.Select(fmt => new FormatPattern(fmt.ToString()));
        static readonly Parser<Pattern> formatPart = Parse.Char('%').Then(_ => _formatPart);

        static readonly Parser<string> varNamePart = Parse.Char('=').Then(_ => varName);

        static readonly Parser<IEnumerable<char>> flagsPart = Parse.Chars('?', '<', '>').Many();

        static readonly Parser<LineSection> captureSection =
            from part in regexPart.Or(formatPart)
            from capt in varNamePart.Optional()
            from flags in flagsPart
            select new CaptureSection(part, capt.GetOrDefault(), flags);

        static readonly Parser<IEnumerable<char>> open = Parse.String("{{");
        static readonly Parser<IEnumerable<char>> close = Parse.String("}}");

        static readonly Parser<LineSection> textSection = Parse.AnyChar.Many().Text().Except(open.Or(close)).Select(x => new TextSection(x));

        static readonly Parser<TemplateLine> templateLine =
            captureSection.Contained(open, close)
            .Or(textSection)
            .Many()
            .Select(x => new TemplateLine(x));

        static readonly Parser<char> directive = Parse.Char('#');

        static readonly Parser<TemplateLine> fileNameLine = directive.Then(_ => templateLine);

        static readonly Parser<FilterLine> _filterLine = regexPart.Then(
            pattern => Parse.AnyChar.Many().Text().Select(replace => new FilterLine(pattern, replace)));
        static readonly Parser<FilterLine> filterLine = directive.Then(_ => _filterLine);

        static readonly Parser<TemplateFile> templateFile =
            from fnl in fileNameLine.Until(Parse.LineEnd).Optional()
            from filters in filterLine.Until(Parse.LineEnd)
    }
}
