using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    /// <summary>
    /// This class implements a recursive descent parser for the todl language
    /// </summary>
    public sealed partial class Parser
    {
        private readonly SyntaxTree syntaxTree;
        private readonly List<Diagnostic> diagnostics = new();
        private readonly List<Directive> directives = new();
        private readonly List<Member> members = new();
        private int position = 0;

        private IReadOnlyList<SyntaxToken> SyntaxTokens => syntaxTree.SyntaxTokens;

        private SyntaxToken Current => Seek(0);
        private SyntaxToken Peak => Seek(1);

        public IReadOnlyList<Diagnostic> Diagnostics
        {
            get
            {
                var lexerDiagnostics = SyntaxTokens.Where(t => t.Diagnostic != null).Select(t => t.Diagnostic);
                if (lexerDiagnostics.Any())
                {
                    return lexerDiagnostics.ToList();
                }

                return this.diagnostics;
            }
        }

        public IReadOnlyList<Directive> Directives => directives;
        public IReadOnlyList<Member> Members => members;

        private SyntaxToken Seek(int offset)
        {
            var index = this.position + offset;
            if (index >= this.SyntaxTokens.Count)
            {
                return this.SyntaxTokens[SyntaxTokens.Count - 1];
            }

            return this.SyntaxTokens[index];
        }

        private SyntaxToken NextToken()
        {
            var current = this.Current;
            ++this.position;
            return current;
        }

        private SyntaxToken ExpectToken(SyntaxKind syntaxKind)
        {
            if (this.Current.Kind == syntaxKind)
            {
                return this.NextToken();
            }

            return ReportUnexpectedToken(syntaxKind);
        }

        internal Parser(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
        }

        public void Parse()
        {
            while (Current.Kind == SyntaxKind.ImportKeywordToken)
            {
                directives.Add(ParseDirective());
            }

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                members.Add(ParseMember());
            }
        }

        private Expression ParsePrimaryExpression()
        {
            Expression baseExpression;

            switch (Current.Kind)
            {
                case SyntaxKind.NumberToken:
                case SyntaxKind.StringToken:
                case SyntaxKind.TrueKeywordToken:
                case SyntaxKind.FalseKeywordToken:
                    baseExpression = new LiteralExpression()
                    {
                        SyntaxTree = syntaxTree,
                        LiteralToken = ExpectToken(Current.Kind)
                    };
                    break;
                case SyntaxKind.OpenParenthesisToken:
                    baseExpression = ParseTrailingUnaryExpression(this.ParseParethesizedExpression());
                    break;
                case SyntaxKind.NewKeywordToken:
                    baseExpression = ParseNewExpression();
                    break;
                case SyntaxKind.IdentifierToken:
                default:
                    var nameExpression = ParseNameExpression();
                    baseExpression = ParseTrailingUnaryExpression(nameExpression);

                    break;
            }

            while (true)
            {
                if (AssignmentExpression.AssignmentOperators.Contains(Current.Kind))
                {
                    baseExpression = ParseAssignmentExpression(baseExpression);
                }
                else if (Current.Kind == SyntaxKind.DotToken && Peak.Kind == SyntaxKind.IdentifierToken)
                {
                    baseExpression = new MemberAccessExpression()
                    {
                        SyntaxTree = syntaxTree,
                        BaseExpression = baseExpression,
                        DotToken = ExpectToken(SyntaxKind.DotToken),
                        MemberIdentifierToken = ExpectToken(SyntaxKind.IdentifierToken)
                    };
                }
                else if (Current.Kind == SyntaxKind.OpenParenthesisToken)
                {
                    baseExpression = ParseFunctionCallExpression(baseExpression);
                    break;
                }
                else
                {
                    break;
                }
            }

            return baseExpression;
        }

        private SyntaxToken ReportUnexpectedToken(SyntaxKind expectedSyntaxKind)
        {
            var diagnostic = new Diagnostic()
            {
                Message = $"Unexpected token found: {Current.Text}. Expecting {expectedSyntaxKind}",
                Level = DiagnosticLevel.Error,
                TextLocation = Current.GetTextLocation(),
                ErrorCode = ErrorCode.UnexpectedToken
            };
            diagnostics.Add(diagnostic);

            // return a fake syntax token of the expected kind, with a text span at the current location with 0 length 
            return new()
            {
                Kind = expectedSyntaxKind,
                Text = syntaxTree.SourceText.GetTextSpan(Current.Text.Start, 0),
                LeadingTrivia = Array.Empty<SyntaxTrivia>(),
                TrailingTrivia = Array.Empty<SyntaxTrivia>(),
                Missing = true,
                Diagnostic = diagnostic
            };
        }
    }
}
