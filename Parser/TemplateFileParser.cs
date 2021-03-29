// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace ReverseTemplate.Parser {
    public class TemplateFileParser {
        static readonly TextParser<Unit> newLine = Span.EqualTo("\r\n").Value(Unit.Value).Or(Character.In('\n', '\r').Value(Unit.Value));
        static readonly TextParser<char> notNewLine = Character.ExceptIn('\n', '\r');

        static readonly TextParser<TextSpan> open = Span.EqualTo("{{");
        static readonly TextParser<TextSpan> close = Span.EqualTo("}}");

        static readonly TextParser<TextSpan> varName = Span.MatchedBy(Character.LetterOrDigit.Or(Character.EqualTo('_')).AtLeastOnce());
        static readonly TextParser<VariablePart> arrayVarPart =
            from vn in varName
            from _ in Span.EqualTo("[]")
            select new ArrayVariablePart(vn.ToStringValue()) as VariablePart;
        static readonly TextParser<VariablePart> objVarPart = varName.Select(vn => new ObjectVariablePart(vn.ToStringValue()) as VariablePart);
        static readonly TextParser<VariablePart[]> _varPath = arrayVarPart.Try().Or(objVarPart).AtLeastOnceDelimitedBy(Character.EqualTo('.'));
        static readonly TextParser<VariablePart[]> varPath = Character.EqualTo('=').IgnoreThen(_varPath);

        static readonly TextParser<char> escape = Character.EqualTo('\\');
        static readonly TextParser<char> escapable = Character.In('\\', '/');
        static readonly TextParser<char> notEscapable = Character.ExceptIn('\\', '/');

        static readonly TextParser<char> regexChars = escape.IgnoreThen(escapable).Try().Or(escape).Or(notEscapable);

        // can't capture the whole original span since we need to transform \\ and \/
        static readonly TextParser<Pattern> _regexPart = regexChars.Many().Select(c => new RegexPattern(new string(c)) as Pattern);
        static readonly TextParser<Pattern> regexPart = _regexPart.Between(Character.EqualTo('/'), Character.EqualTo('/'));

        static readonly TextParser<Pattern> _formatPart = notNewLine.Select(fmt => new FormatPattern(fmt.ToString()) as Pattern);
        static readonly TextParser<Pattern> formatPart = Character.EqualTo('%').IgnoreThen(_formatPart);

        static readonly TextParser<char[]> flagsPart = Character.In('?', '<', '>', '|').Many();

        static readonly TextParser<LineSection> _captureSection =
            from part in regexPart.Or(formatPart)
            from capt in varPath.OptionalOrDefault(Array.Empty<VariablePart>())
            from flags in flagsPart
            select new CaptureSection(part, capt, flags) as LineSection;

        static readonly TextParser<LineSection> textSection = Span.MatchedBy(Parse.Not(open.Or(close)).IgnoreThen(notNewLine).AtLeastOnce())
            .Select(x => new TextSection(x.ToStringValue()) as LineSection);

        static readonly TextParser<TemplateLine> _templateLine =
            textSection
            .Or(_captureSection.Between(open, close))
            .Many()
            .Select(x => new TemplateLine(x));

        static readonly TextParser<Unit> directive = Character.EqualTo('#').Value(Unit.Value);

        // in case of confusion between filename line and filter
        static readonly TextParser<TemplateLine> fileNameLine = Parse.Not(Span.EqualTo("#/")).IgnoreThen(_templateLine.Between(directive, newLine));

        static readonly TextParser<FilterLine> _filterLine =
            from pattern in regexPart
            from replace in Span.MatchedBy(notNewLine.IgnoreMany())
            select new FilterLine(pattern, replace.ToStringValue());
        static readonly TextParser<FilterLine> filterLine = _filterLine.Between(directive, newLine);

        static readonly TextParser<TemplateFile> templateFile =
            from fnl in fileNameLine!.OptionalOrDefault()
            from filters in filterLine.Many()
            from lines in _templateLine.ManyDelimitedBy(newLine)
            from _ in newLine.OptionalOrDefault()
            select new TemplateFile(lines, filters: filters, fileNameLine: fnl);

        public static TextParser<TemplateLine> TemplateLine => _templateLine;
        public static TextParser<TemplateFile> TemplateFile => templateFile;
    }
}
