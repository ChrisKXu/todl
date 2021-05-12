using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Diagnostics;

namespace Todl.CodeAnalysis
{
    /// <summary>
    /// This class implements a recursive descent parser for the todl language
    /// </summary>
    public sealed class Parser
    {
        private readonly SyntaxTree syntaxTree;
        private readonly Lexer lexer;
        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();
        private int position = 0;

        private IReadOnlyList<SyntaxToken> SyntaxTokens => this.lexer.SyntaxTokens;

        private SyntaxToken Current => this.Seek(0);
        private SyntaxToken Peak => this.Seek(1);

        public IReadOnlyCollection<Diagnostic> Diagnostics
        {
            get
            {
                if (this.lexer.Diagnostics.Any())
                {
                    return this.lexer.Diagnostics;
                }

                return this.diagnostics;
            }
        }

        private SyntaxToken Seek(int offset)
        {
            var index = this.position + offset;
            if (index >= this.SyntaxTokens.Count)
            {
                return this.SyntaxTokens.Last();
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

            return null;
        }

        public Parser(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
            this.lexer = new Lexer(syntaxTree);
        }

        public void Parse()
        {
            this.lexer.Lex();

            if (this.lexer.Diagnostics.Any())
            {
                return;
            }

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                ParseExpression();
            }
        }

        private Expression ParseExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.NumberToken:
                    return ParseBinaryExpression();
            }
            
            throw new NotImplementedException();
        }

        private Expression ParseBinaryExpression(int parentPrecedence = 0)
        {
            var left = this.ParsePrimaryExpression();

            while (true)
            {
                var precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                {
                    break;
                }
                
                var operatorToken = this.NextToken();
                var right = ParseBinaryExpression(precedence);

                left = new BinaryExpression(this.syntaxTree, left, operatorToken, right);
            }

            return left;
        }

        private Expression ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.NumberToken:
                    return new LiteralExpression(this.syntaxTree, this.ExpectToken(SyntaxKind.NumberToken));
            }

            throw new NotImplementedException($"{Current.Kind} is not recognised");
        }
    }
}