using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.Evaluation
{
    /// <summary>
    /// An evaluator evaluates expressions and statements and give out results as output
    /// </summary>
    public class Evaluator
    {
        private readonly SourceText sourceText;

        public Evaluator(SourceText sourceText)
        {
            this.sourceText = sourceText;
        }

        public EvaluatorResult Evaluate()
        {
            var syntaxTree = new SyntaxTree(this.sourceText);
            var parser = new Parser(syntaxTree);
            parser.Lex();
            var binaryExpression = parser.ParseBinaryExpression();

            var diagnosticsOutput = parser.Diagnostics.Select(diagnostics => diagnostics.Message).ToList();

            return new EvaluatorResult()
            {
                DiagnosticsOutput = diagnosticsOutput,
                EvaluationOutput = EvaluateExpression(binaryExpression).ToString()
            };
        }

        private int EvaluateExpression(Expression expression)
        {
            switch (expression)
            {
                case LiteralExpression literalExpression:
                    return int.Parse(literalExpression.Text.ToString());
                case BinaryExpression binaryExpression:
                    return EvaluateBinaryExpression(binaryExpression);
                case ParethesizedExpression parethesizedExpression:
                    return EvaluateExpression(parethesizedExpression.InnerExpression);
            }

            throw new NotSupportedException("Expression type not supported");
        }

        private int EvaluateBinaryExpression(BinaryExpression binaryExpression)
        {
            var left = EvaluateExpression(binaryExpression.Left);
            var right = EvaluateExpression(binaryExpression.Right);

            switch (binaryExpression.Operator.Kind)
            {
                case SyntaxKind.PlusToken:
                    return left + right;
                case SyntaxKind.MinusToken:
                    return left - right;
                case SyntaxKind.StarToken:
                    return left * right;
                case SyntaxKind.SlashToken:
                    return left / right;
            }

            throw new NotSupportedException($"Operator {binaryExpression.Operator.Text} not supported");
        }
    }
}
