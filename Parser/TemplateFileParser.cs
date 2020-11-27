// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sprache;

namespace ReverseTemplate.Parser {
    public class TemplateFileParser {
        static readonly Parser<IEnumerable<char>> open = Parse.String("{{");
        static readonly Parser<IEnumerable<char>> close = Parse.String("}}");

        static readonly Parser<string> varName = Parse.LetterOrDigit.Or(Parse.Char('_')).AtLeastOnce().Text();
        static readonly Parser<VariablePart> arrayVarPart =
            from vn in varName
            from _ in Parse.String("[]")
            select new ArrayVariablePart(vn);
        static readonly Parser<VariablePart> objVarPart = varName.Select(vn => new ObjectVariablePart(vn));
        static readonly Parser<IEnumerable<VariablePart>> varPath = arrayVarPart.Or(objVarPart).DelimitedBy(Parse.Char('.'));
        static readonly Parser<char> escape = Parse.Char('\\');
        static readonly Parser<char> escapable = Parse.Chars('\\', '/');
        static readonly Parser<char> notEscapable = Parse.CharExcept("\\/");

        static readonly Parser<char> regexChars = escape.Then(_ => escapable).Or(escape).Or(notEscapable);

        static readonly Parser<Pattern> _regexPart = regexChars.Many().Text().Select(regex => new RegexPattern(regex));
        static readonly Parser<Pattern> regexPart = _regexPart.Contained(Parse.Char('/'), Parse.Char('/'));

        static readonly Parser<Pattern> _formatPart = Parse.AnyChar.Select(fmt => new FormatPattern(fmt.ToString()));
        static readonly Parser<Pattern> formatPart = Parse.Char('%').Then(_ => _formatPart);

        static readonly Parser<IEnumerable<VariablePart>> varPathPart = Parse.Char('=').Then(_ => varPath);

        static readonly Parser<IEnumerable<char>> flagsPart = Parse.Chars('?', '<', '>').Many();

        static readonly Parser<LineSection> _captureSection =
            from part in regexPart.Or(formatPart)
            from capt in varPathPart.Optional()
            from flags in flagsPart
            select new CaptureSection(part, capt.GetOrElse(Enumerable.Empty<VariablePart>()), flags);

        static readonly Parser<LineSection> textSection = Parse.AnyChar.Except(open.Or(close).Or(Parse.LineTerminator)).Many().Text().Select(x => new TextSection(x));

        static readonly Parser<TemplateLine> _templateLine =
            textSection
            .Or(_captureSection.Contained(open, close))
            .Many()
            .Select(x => new TemplateLine(x));
        static readonly Parser<TemplateLine> templateLine =
            from tl in _templateLine
            from _ in Parse.LineTerminator
            select tl;

        static readonly Parser<char> directive = Parse.Char('#');

        static readonly Parser<TemplateLine> fileNameLine = _templateLine.Contained(directive, Parse.LineEnd);

        static readonly Parser<FilterLine> _filterLine =
            from pattern in regexPart
            from replace in Parse.AnyChar.Except(Parse.LineTerminator).Many().Text()
            select new FilterLine(pattern, replace);
        static readonly Parser<FilterLine> filterLine = _filterLine.Contained(directive, Parse.LineEnd);

        static readonly Parser<TemplateFile> templateFile =
            from fnl in fileNameLine.Optional()
            from filters in filterLine.Many()
            from lines in templateLine.Many().End()
            select new TemplateFile(lines, filters: filters, fileNameLine: fnl.GetOrDefault());

        public static Parser<TemplateLine> TemplateLine => templateLine;
        public static Parser<TemplateFile> TemplateFile => templateFile;
    }
}
