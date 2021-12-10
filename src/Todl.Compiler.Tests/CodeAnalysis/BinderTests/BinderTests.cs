using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    using Binder = Todl.Compiler.CodeAnalysis.Binding.Binder;

    public sealed partial class BinderTests
    {
        private static TBoundExpression BindExpression<TBoundExpression>(
            string inputText)
            where TBoundExpression : BoundExpression
        {
            var expression = SyntaxTree.ParseExpression(SourceText.FromString(inputText));
            var binder = Binder.CreateModuleBinder();
            return binder.BindExpression(expression).As<TBoundExpression>();
        }

        private static TBoundStatement BindStatement<TBoundStatement>(
            string inputText)
            where TBoundStatement : BoundStatement
        {
            var statement = SyntaxTree.ParseStatement(SourceText.FromString(inputText));
            var binder = Binder.CreateModuleBinder();
            return binder.BindStatement(statement) as TBoundStatement;
        }

        private static TBoundMember BindMember<TBoundMember>(
            string inputText)
            where TBoundMember : BoundMember
        {
            var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText));
            var binder = Binder.CreateModuleBinder();
            var member = syntaxTree.Members[0];

            if (member is FunctionDeclarationMember functionDeclarationMember)
            {
                binder.Scope.DeclareFunction(FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember));
            }

            return binder.BindMember(member).As<TBoundMember>();
        }

        [Fact]
        public void TestBindBinaryExpression()
        {
            var boundBinaryExpression = BindExpression<BoundBinaryExpression>("1 + 2 + 3");

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
            var boundConstant = BindExpression<BoundConstant>(input);

            boundConstant.Should().NotBeNull();
            boundConstant.ResultType.Should().Be(TypeSymbol.ClrString);
            boundConstant.Value.Should().Be(expectedOutput);
        }

        [Theory]
        [MemberData(nameof(GetTestBindAssignmentExpressionDataWithEqualsToken))]
        public void TestBindAssignmentExpressionEqualsToken(string input, string variableName, TypeSymbol expectedResultType)
        {
            var expression = SyntaxTree.ParseExpression(SourceText.FromString(input));
            var boundAssignmentExpression =
                Binder.CreateScriptBinder().BindExpression(expression)
                .As<BoundAssignmentExpression>();

            boundAssignmentExpression.Should().NotBeNull();

            var variable = boundAssignmentExpression.Left.As<BoundVariableExpression>().Variable;

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
                const a = 0;
                const b = a + 10;
            }
            ";

            var boundBlockStatement = BindStatement<BoundBlockStatement>(input);

            boundBlockStatement.Should().NotBeNull();
            boundBlockStatement.Statements.Count.Should().Be(2);
            boundBlockStatement.Scope.LookupVariable("a").Type.Should().Be(TypeSymbol.ClrInt32);
            boundBlockStatement.Scope.LookupVariable("b").Type.Should().Be(TypeSymbol.ClrInt32);

            boundBlockStatement.Statements[0].Should().BeOfType<BoundVariableDeclarationStatement>();
            boundBlockStatement.Statements[1].Should().BeOfType<BoundVariableDeclarationStatement>();

            boundBlockStatement.Statements[1]
                .As<BoundVariableDeclarationStatement>()
                .InitializerExpression.Should().BeOfType<BoundBinaryExpression>();
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
            var boundBlockStatement = BindStatement<BoundBlockStatement>(input);

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

            var boundBlockStatement = BindStatement<BoundBlockStatement>(input);

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
            var boundMemberAccessExpression = BindExpression<BoundMemberAccessExpression>("\"abc\".Length");

            boundMemberAccessExpression.MemberInfo.MemberType.Should().Be(MemberTypes.Property);
            boundMemberAccessExpression.MemberName.Should().Be("Length");
            boundMemberAccessExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(int));
            boundMemberAccessExpression.IsStatic.Should().Be(false);
        }

        [Fact]
        public void TestBindMemberAccessExpressionStaticField()
        {
            var boundMemberAccessExpression = BindExpression<BoundMemberAccessExpression>("System.Int32.MaxValue");

            boundMemberAccessExpression.MemberInfo.MemberType.Should().Be(MemberTypes.Field);
            boundMemberAccessExpression.MemberName.Should().Be("MaxValue");
            boundMemberAccessExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(int));
            boundMemberAccessExpression.IsStatic.Should().Be(true);
        }

        [Fact]
        public void TestBoundObjectCreationExpressionWithNoArguments()
        {
            var boundObjectCreationExpression = BindExpression<BoundObjectCreationExpression>("new System.Exception()");

            boundObjectCreationExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(System.Exception));
            boundObjectCreationExpression.ConstructorInfo.Should().NotBeNull();
            boundObjectCreationExpression.BoundArguments.Should().BeEmpty();
        }

        [Theory]
        [InlineData("new System.Exception(\"exception message\")")]
        [InlineData("new System.Exception(message: \"exception message\")")]
        public void TestBoundObjectCreationExpressionWithOneArgument(string inputText)
        {
            var boundObjectCreationExpression = BindExpression<BoundObjectCreationExpression>(inputText);

            boundObjectCreationExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(System.Exception));
            boundObjectCreationExpression.ConstructorInfo.Should().NotBeNull();
            boundObjectCreationExpression.BoundArguments.Count.Should().Be(1);

            var message = boundObjectCreationExpression.BoundArguments[0].As<BoundConstant>();
            message.Value.Should().Be("exception message");
        }
    }
}
