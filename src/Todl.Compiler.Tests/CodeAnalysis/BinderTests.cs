using System;
using System.Collections.Generic;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public sealed class BinderTests
    {
        public static TBoundExpression BindExpression<TBoundExpression>(
            string inputText,
            Binder binder,
            BoundScope scope)
            where TBoundExpression : BoundExpression
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(inputText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var expression = parser.ParseExpression();
            return binder.BindExpression(scope, expression) as TBoundExpression;
        }

        public static TBoundStatement BindStatement<TBoundStatement>(
            string inputText,
            Binder binder,
            BoundScope scope)
            where TBoundStatement : BoundStatement
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(inputText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var statement = parser.ParseStatement();
            return binder.BindStatement(scope, statement) as TBoundStatement;
        }

        [Fact]
        public void TestBindBinaryExpression()
        {
            var boundBinaryExpression = BindExpression<BoundBinaryExpression>(
                inputText: "1 + 2 + 3",
                binder: new Binder(BinderFlags.None),
                scope: BoundScope.GlobalScope);

            boundBinaryExpression.Should().NotBeNull();
            boundBinaryExpression.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryExpression.BoundBinaryOperatorKind.NumericAddition);
            (boundBinaryExpression.Right as BoundConstant).Value.Should().Be(3);

            var left = boundBinaryExpression.Left as BoundBinaryExpression;
            left.Should().NotBeNull();
            (left.Left as BoundConstant).Value.Should().Be(1);
            (left.Right as BoundConstant).Value.Should().Be(2);
            left.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryExpression.BoundBinaryOperatorKind.NumericAddition);
        }

        [Theory]
        [InlineData("\"\"", "")]
        [InlineData("\"abcd\"", "abcd")]
        [InlineData("\"ab\\\"cd\"", "ab\"cd")]
        [InlineData("@\"abcd\"", "abcd")]
        [InlineData("@\"ab\\\"cd\"", "ab\\\"cd")]
        public void TestBindStringConstant(string input, string expectedOutput)
        {
            var boundConstant = BindExpression<BoundConstant>(
                inputText: input,
                binder: new Binder(BinderFlags.None),
                scope: BoundScope.GlobalScope);

            boundConstant.Should().NotBeNull();
            boundConstant.ResultType.Should().Be(TypeSymbol.ClrString);
            boundConstant.Value.Should().Be(expectedOutput);
        }

        [Theory]
        [MemberData(nameof(GetTestBindAssignmentExpressionDataWithEqualsToken))]
        public void TestBindAssignmentExpressionEqualsToken(string input, string variableName, TypeSymbol expectedResultType)
        {
            var binder = new Binder(BinderFlags.AllowVariableDeclarationInAssignment);
            var boundAssignmentExpression = BindExpression<BoundAssignmentExpression>(
                inputText: input,
                binder: binder,
                scope: BoundScope.GlobalScope);

            binder.Diagnostics.Should().BeEmpty();
            boundAssignmentExpression.Should().NotBeNull();

            var variable = (boundAssignmentExpression.Left as BoundVariableExpression).Variable;

            variable.Name.Should().Be(variableName);
            variable.ReadOnly.Should().BeFalse();
            boundAssignmentExpression.Operator.SyntaxKind.Should().Be(SyntaxKind.EqualsToken);
            variable.Type.Should().Be(expectedResultType);
            boundAssignmentExpression.ResultType.Should().Be(expectedResultType);
        }

        public static IEnumerable<object[]> GetTestBindAssignmentExpressionDataWithEqualsToken()
        {
            yield return new object[] { "n = 0", "n", TypeSymbol.ClrInt32 };
            yield return new object[] { "abcd = \"abcde\"", "abcd", TypeSymbol.ClrString };
        }

        [Fact]
        public void TestBindBlockStatement()
        {
            var input = @"
            {
                a = 0;
                b = a + 10;
            }
            ";
            var binder = new Binder(BinderFlags.AllowVariableDeclarationInAssignment);
            var boundBlockStatement = BindStatement<BoundBlockStatement>(
                inputText: input,
                binder: binder,
                scope: BoundScope.GlobalScope);

            boundBlockStatement.Should().NotBeNull();
            boundBlockStatement.Statements.Count.Should().Be(2);
            boundBlockStatement.Scope.LookupVariable("a").Type.Should().Be(TypeSymbol.ClrInt32);
            boundBlockStatement.Scope.LookupVariable("b").Type.Should().Be(TypeSymbol.ClrInt32);

            var firstExpression = boundBlockStatement.Statements[0] as BoundExpressionStatement;
            (firstExpression.Expression as BoundAssignmentExpression).Should().NotBeNull();

            var secondExpression = boundBlockStatement.Statements[1] as BoundExpressionStatement;
            var binaryExpression = (secondExpression.Expression as BoundAssignmentExpression).Right as BoundBinaryExpression;
            binaryExpression.Should().NotBeNull();
        }

        [Fact]
        public void TestBindVariableDeclarationStatementBasic()
        {
            var input = @"
            {
                const a = 0;
                let b = a + 4;
            }
            ";
            var binder = new Binder(BinderFlags.None);
            var boundBlockStatement = BindStatement<BoundBlockStatement>(
                inputText: input,
                binder: binder,
                scope: BoundScope.GlobalScope);

            boundBlockStatement.Should().NotBeNull();
            boundBlockStatement.Statements.Count.Should().Be(2);
            boundBlockStatement.Scope.LookupVariable("a").Type.Should().Be(TypeSymbol.ClrInt32);
            boundBlockStatement.Scope.LookupVariable("b").Type.Should().Be(TypeSymbol.ClrInt32);
        }

        [Fact]
        public void TestBindVariableDeclarationStatementWithNestedScope()
        {
            var input = @"
            {
                const a = 0;
                let b = a + 4;
                {
                    let a = true;
                    b = 20;
                }
                b = a + 5;
            }
            ";
            var binder = new Binder(BinderFlags.None);
            var boundBlockStatement = BindStatement<BoundBlockStatement>(
                inputText: input,
                binder: binder,
                scope: BoundScope.GlobalScope);

            boundBlockStatement.Should().NotBeNull();
            boundBlockStatement.Statements.Count.Should().Be(4);

            var scope = boundBlockStatement.Scope;
            var childScope = (boundBlockStatement.Statements[2] as BoundBlockStatement).Scope;
            childScope.Parent.Should().Be(scope);

            scope.LookupVariable("a").Type.Should().Be(TypeSymbol.ClrInt32);
            scope.LookupVariable("b").Type.Should().Be(TypeSymbol.ClrInt32);

            childScope.LookupVariable("a").Type.Should().Be(TypeSymbol.ClrBoolean);
            childScope.LookupVariable("b").Type.Should().Be(TypeSymbol.ClrInt32);
        }

        [Fact]
        public void TestBindImportDirectiveSingle()
        {
            var binder = new Binder(BinderFlags.None);
            var syntaxTree = new SyntaxTree(SourceText.FromString("import { Task } from System.Threading.Tasks;"));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var importDirective = parser.ParseDirective().As<ImportDirective>();
            var boundImportDirective = binder.BindImportDirective(importDirective);
            var taskType = boundImportDirective.ImportedTypes["Task"];
            taskType.Should().Be(typeof(System.Threading.Tasks.Task));
            boundImportDirective.Namespace.Should().Be("System.Threading.Tasks");
        }
    }
}
