using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace ReverseTemplate.Parser {
    public enum TemplateToken {
        [Token(Example = "{{")]
        DoubleLeftBracket,
        [Token(Example = "}}")]
        DoubleRightBracket,
        RawText,
        Capture,
        TemplateLine,
    }

    public static class TemplateParser {
        static TextParser<TextSpan> VarName { get; } = Span.MatchedBy(Character.LetterOrDigit.Or(Character.EqualTo('_')).AtLeastOnce());

        static TextParser<char> Escape { get; } = Character.EqualTo('\\');
        static TextParser<char> Escapable { get; } = Character.In('\\', '/');
        static TextParser<char> NotEscapable { get; } = Character.ExceptIn('\\', '/');

        static TextParser<char> RegexChars { get; } =
            NotEscapable
                .Or(Escape.IgnoreThen(Escapable).Try())
                .Or(Escape);

        static TextParser<Pattern> RegexPart { get; } =
            from _beginSlash in Character.EqualTo('/')
            from regex in Span.MatchedBy(RegexChars.IgnoreMany())
            from _endSlash in Character.EqualTo('/')
            select new RegexPattern(regex.ToStringValue()) as Pattern;

        static TextParser<Pattern> FormatPart { get; } =
            from _pct in Character.EqualTo('%')
            from fmt in Character.AnyChar
            select new FormatPattern(fmt.ToString()) as Pattern;

        static TextParser<TextSpan> VarNamePart { get; } = Character.EqualTo('=').IgnoreThen(VarName);

        static TextParser<char[]> FlagsPart { get; } = Character.In('?').Many();

        static TextParser<CaptureSection> CaptureBlock { get; } =
            from part in RegexPart.Or(FormatPart)
            from capt in VarNamePart.Optional()
            from flags in FlagsPart
            select new CaptureSection(part, capt?.ToStringValue(), flags);

        static TextParser<char> LeftBracket { get; } = Character.EqualTo('{');
        static TextParser<char> RightBracket { get; } = Character.EqualTo('}');
        static TextParser<char> NotLeftBracket { get; } = Character.ExceptIn('{');
        static TextParser<char> NotRightBracket { get; } = Character.ExceptIn('}');
        static TextParser<char> Brackets { get; } = Character.In('{', '}');
        static TextParser<char> NotBrackets { get; } = Character.ExceptIn('{', '}');

        static TextParser<Unit> RawTextPart { get; } =
            NotBrackets.Value(Unit.Value)
            .Or(Brackets.AtEnd().Value(Unit.Value).Try())
            .Or(LeftBracket.IgnoreThen(NotLeftBracket).Value(Unit.Value).Try())
            .Or(RightBracket.IgnoreThen(NotRightBracket).Value(Unit.Value).Try());

        static Tokenizer<TemplateToken> TemplateTokenizer { get; } = new TokenizerBuilder<TemplateToken>()
            .Match(Span.EqualTo("{{"), TemplateToken.DoubleLeftBracket)
            .Match(Span.EqualTo("}}"), TemplateToken.DoubleRightBracket)
            .Match(RawTextPart.IgnoreMany(), TemplateToken.RawText)
            .Build();

        static TokenListParser<TemplateToken, LineSection> CaptureToken { get; } =
            Token.EqualTo(TemplateToken.RawText).AtLeastOnce()
                .Between(Token.EqualTo(TemplateToken.DoubleLeftBracket), Token.EqualTo(TemplateToken.DoubleRightBracket))
                .Select(tokens => {
                    var texts = tokens.Select(x => x.ToStringValue());
                    return CaptureBlock.Parse(string.Concat(texts)) as LineSection;
                });

        static TokenListParser<TemplateToken, LineSection> TextToken { get; } =
            Token.EqualTo(TemplateToken.RawText)
                .Select(t => new TextSection(t.ToStringValue()) as LineSection);

        static TokenListParser<TemplateToken, LineSection[]> TemplateLineToken { get; } =
            CaptureToken.Or(TextToken).Many().AtEnd();

        public static bool TryParse(string line, [NotNullWhen(true)] out TemplateLine? outLine, out string? error, out Position errorPosition) {
            var tokens = TemplateTokenizer.TryTokenize(line);
            if (!tokens.HasValue) {
                outLine = null;
                error = tokens.ErrorMessage;
                errorPosition = tokens.ErrorPosition;
                return false;
            }

            var parsed = TemplateLineToken.TryParse(tokens.Value);
            if (!parsed.HasValue) {
                outLine = null;
                error = parsed.ErrorMessage;
                errorPosition = parsed.ErrorPosition;
                return false;
            }

            outLine = new TemplateLine(parsed.Value);
            error = null;
            errorPosition = Position.Empty;
            return true;
        }
    }
}
