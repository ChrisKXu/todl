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
    public sealed partial class BinderTests
    {
        private static TBoundExpression BindExpression<TBoundExpression>(
            string inputText,
            BinderFlags binderFlags,
            BoundScope scope)
            where TBoundExpression : BoundExpression
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(inputText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var expression = parser.ParseExpression();
            var binder = new Binder(binderFlags, syntaxTree.ClrTypeCache.CreateView(Array.Empty<ImportDirective>()));
            return binder.BindExpression(scope, expression).As<TBoundExpression>();
        }

        private static TBoundStatement BindStatement<TBoundStatement>(
            string inputText,
            BinderFlags binderFlags,
            BoundScope scope)
            where TBoundStatement : BoundStatement
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(inputText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var statement = parser.ParseStatement();
            var binder = new Binder(binderFlags, syntaxTree.ClrTypeCache.CreateView(Array.Empty<ImportDirective>()));
            return binder.BindStatement(scope, statement) as TBoundStatement;
        }

        private static TBoundMember BindMember<TBoundMember>(
            string inputText,
            BinderFlags binderFlags,
            BoundScope scope)
            where TBoundMember : BoundMember
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(inputText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var member = parser.ParseMember();
            var binder = new Binder(binderFlags, syntaxTree.ClrTypeCache.CreateView(Array.Empty<ImportDirective>()));
            return binder.BindMember(scope, member).As<TBoundMember>();
        }

        [Fact]
        public void TestBindBinaryExpression()
        {
            var boundBinaryExpression = BindExpression<BoundBinaryExpression>(
                inputText: "1 + 2 + 3",
                binderFlags: BinderFlags.None,
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
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundConstant.Should().NotBeNull();
            boundConstant.ResultType.Should().Be(TypeSymbol.ClrString);
            boundConstant.Value.Should().Be(expectedOutput);
        }

        [Theory]
        [MemberData(nameof(GetTestBindAssignmentExpressionDataWithEqualsToken))]
        public void TestBindAssignmentExpressionEqualsToken(string input, string variableName, TypeSymbol expectedResultType)
        {
            var boundAssignmentExpression = BindExpression<BoundAssignmentExpression>(
                inputText: input,
                binderFlags: BinderFlags.AllowVariableDeclarationInAssignment,
                scope: BoundScope.GlobalScope);

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

            var boundBlockStatement = BindStatement<BoundBlockStatement>(
                inputText: input,
                binderFlags: BinderFlags.AllowVariableDeclarationInAssignment,
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
            var boundBlockStatement = BindStatement<BoundBlockStatement>(
                inputText: input,
                binderFlags: BinderFlags.None,
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

            var boundBlockStatement = BindStatement<BoundBlockStatement>(
                inputText: input,
                binderFlags: BinderFlags.None,
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
        public void TestBindMemberAccessExpressionInstanceProperty()
        {
            var boundMemberAccessExpression = BindExpression<BoundMemberAccessExpression>(
                inputText: "\"abc\".Length",
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundMemberAccessExpression.BoundMemberAccessKind.Should().Be(BoundMemberAccessKind.Property);
            boundMemberAccessExpression.MemberName.Text.Should().Be("Length");
            boundMemberAccessExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(int));
            boundMemberAccessExpression.IsStatic.Should().Be(false);
        }

        [Fact]
        public void TestBindMemberAccessExpressionStaticField()
        {
            var boundMemberAccessExpression = BindExpression<BoundMemberAccessExpression>(
                inputText: "System.Int32.MaxValue",
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundMemberAccessExpression.BoundMemberAccessKind.Should().Be(BoundMemberAccessKind.Field);
            boundMemberAccessExpression.MemberName.Text.Should().Be("MaxValue");
            boundMemberAccessExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(int));
            boundMemberAccessExpression.IsStatic.Should().Be(true);
        }

        [Fact]
        public void TestBindFunctionCallExpressionWithNoArguments()
        {
            var boundFunctionCallExpression = BindExpression<BoundFunctionCallExpression>(
                inputText: "100.ToString()",
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundFunctionCallExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(string));
            boundFunctionCallExpression.MethodInfo.Name.Should().Be("ToString");
            boundFunctionCallExpression.IsStatic.Should().Be(false);
        }

        [Fact]
        public void TestBindFunctionCallExpressionWithOnePositionalArgument()
        {
            var boundFunctionCallExpression = BindExpression<BoundFunctionCallExpression>(
                inputText: "System.Math.Abs(-10)",
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundFunctionCallExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(int));
            boundFunctionCallExpression.MethodInfo.Name.Should().Be("Abs");
            boundFunctionCallExpression.IsStatic.Should().Be(true);
            boundFunctionCallExpression.BoundArguments.Count.Should().Be(1);

            var argument = boundFunctionCallExpression.BoundArguments[0].As<BoundUnaryExpression>();
            argument.Operator.BoundUnaryOperatorKind.Should().Be(BoundUnaryExpression.BoundUnaryOperatorKind.Negation);
            argument.Operand.As<BoundConstant>().Value.Should().Be(10);
        }

        [Fact]
        public void TestBindFunctionCallExpressionWithOneNamedArgument()
        {
            var boundFunctionCallExpression = BindExpression<BoundFunctionCallExpression>(
                inputText: "100.ToString(format: \"G\")",
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundFunctionCallExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(string));
            boundFunctionCallExpression.MethodInfo.Name.Should().Be("ToString");
            boundFunctionCallExpression.IsStatic.Should().Be(false);
            boundFunctionCallExpression.BoundArguments.Count.Should().Be(1);

            var argument = boundFunctionCallExpression.BoundArguments[0].As<BoundConstant>();
            argument.Value.Should().Be("G");
        }

        [Fact]
        public void TestBindFunctionCallExpressionWithMultiplePositionalArguments()
        {
            var boundFunctionCallExpression = BindExpression<BoundFunctionCallExpression>(
                inputText: "\"abcde\".IndexOf(\"ab\", 1, 2)",
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundFunctionCallExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(int));
            boundFunctionCallExpression.MethodInfo.Name.Should().Be("IndexOf");
            boundFunctionCallExpression.IsStatic.Should().Be(false);

            var boundArguments = boundFunctionCallExpression.BoundArguments;
            boundArguments.Count.Should().Be(3);
            boundArguments[0].As<BoundConstant>().Value.Should().Be("ab");
            boundArguments[1].As<BoundConstant>().Value.Should().Be(1);
            boundArguments[2].As<BoundConstant>().Value.Should().Be(2);
        }

        [Theory]
        [InlineData("\"abcde\".Substring(startIndex: 1, length: 2)")]
        [InlineData("\"abcde\".Substring(length: 2, startIndex: 1)")]
        public void TestBindFunctionCallExpressionWithMultipleNamedArguments(string inputText)
        {
            var boundFunctionCallExpression = BindExpression<BoundFunctionCallExpression>(
                inputText: inputText,
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundFunctionCallExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(string));
            boundFunctionCallExpression.MethodInfo.Name.Should().Be("Substring");
            boundFunctionCallExpression.IsStatic.Should().Be(false);

            var boundArguments = boundFunctionCallExpression.BoundArguments;
            boundArguments.Count.Should().Be(2);
            boundArguments[0].As<BoundConstant>().Value.Should().Be(1);
            boundArguments[1].As<BoundConstant>().Value.Should().Be(2);
        }

        [Fact]
        public void TestBoundObjectCreationExpressionWithNoArguments()
        {
            var boundObjectCreationExpression = BindExpression<BoundObjectCreationExpression>(
                inputText: "new System.Exception()",
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundObjectCreationExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(System.Exception));
            boundObjectCreationExpression.ConstructorInfo.Should().NotBeNull();
            boundObjectCreationExpression.BoundArguments.Should().BeEmpty();
        }

        [Theory]
        [InlineData("new System.Exception(\"exception message\")")]
        [InlineData("new System.Exception(message: \"exception message\")")]
        public void TestBoundObjectCreationExpressionWithOneArgument(string inputText)
        {
            var boundObjectCreationExpression = BindExpression<BoundObjectCreationExpression>(
                inputText: inputText,
                binderFlags: BinderFlags.None,
                scope: BoundScope.GlobalScope);

            boundObjectCreationExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(System.Exception));
            boundObjectCreationExpression.ConstructorInfo.Should().NotBeNull();
            boundObjectCreationExpression.BoundArguments.Count.Should().Be(1);

            var message = boundObjectCreationExpression.BoundArguments[0].As<BoundConstant>();
            message.Value.Should().Be("exception message");
        }
    }
}
