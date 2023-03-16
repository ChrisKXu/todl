﻿using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis;
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
        [Theory]
        [MemberData(nameof(GetTestBindAssignmentExpressionDataWithEqualsToken))]
        public void TestBindAssignmentExpressionEqualsToken(string input, string variableName, TypeSymbol expectedResultType)
        {
            var expression = SyntaxTree.ParseExpression(SourceText.FromString(input), TestDefaults.DefaultClrTypeCache);
            var boundAssignmentExpression =
                Binder.CreateScriptBinder(TestDefaults.DefaultClrTypeCache).BindExpression(expression)
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
            yield return new object[] { "n = 0", "n", TestDefaults.DefaultClrTypeCache.BuiltInTypes.Int32 };
            yield return new object[] { "abcd = \"abcde\"", "abcd", TestDefaults.DefaultClrTypeCache.BuiltInTypes.String };
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

            var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);

            boundBlockStatement.Should().NotBeNull();
            boundBlockStatement.Statements.Count.Should().Be(2);
            boundBlockStatement.Scope.LookupVariable("a").Type.SpecialType.Should().Be(SpecialType.ClrInt32);
            boundBlockStatement.Scope.LookupVariable("b").Type.SpecialType.Should().Be(SpecialType.ClrInt32);

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
            var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);

            boundBlockStatement.Should().NotBeNull();
            boundBlockStatement.Statements.Count.Should().Be(2);
            boundBlockStatement.Scope.LookupVariable("a").Type.SpecialType.Should().Be(SpecialType.ClrInt32);
            boundBlockStatement.Scope.LookupVariable("b").Type.SpecialType.Should().Be(SpecialType.ClrInt32);
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

            var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);

            boundBlockStatement.Should().NotBeNull();
            boundBlockStatement.Statements.Count.Should().Be(4);

            var scope = boundBlockStatement.Scope;
            var childScope = (boundBlockStatement.Statements[2] as BoundBlockStatement).Scope;
            childScope.Parent.Should().Be(scope);

            scope.LookupVariable("a").Type.SpecialType.Should().Be(SpecialType.ClrInt32);
            scope.LookupVariable("b").Type.SpecialType.Should().Be(SpecialType.ClrInt32);

            childScope.LookupVariable("a").Type.SpecialType.Should().Be(SpecialType.ClrBoolean);
            childScope.LookupVariable("b").Type.SpecialType.Should().Be(SpecialType.ClrInt32);
        }

        [Fact]
        public void TestBindMemberAccessExpressionInstanceProperty()
        {
            var boundMemberAccessExpression = TestUtils.BindExpression<BoundMemberAccessExpression>("\"abc\".Length");

            boundMemberAccessExpression.MemberInfo.MemberType.Should().Be(MemberTypes.Property);
            boundMemberAccessExpression.MemberName.Should().Be("Length");
            boundMemberAccessExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
            boundMemberAccessExpression.IsStatic.Should().Be(false);
        }

        [Fact]
        public void TestBindMemberAccessExpressionStaticField()
        {
            var boundMemberAccessExpression = TestUtils.BindExpression<BoundMemberAccessExpression>("System.Int32.MaxValue");

            boundMemberAccessExpression.MemberInfo.MemberType.Should().Be(MemberTypes.Field);
            boundMemberAccessExpression.MemberName.Should().Be("MaxValue");
            boundMemberAccessExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
            boundMemberAccessExpression.IsStatic.Should().Be(true);
        }

        [Fact]
        public void TestBoundObjectCreationExpressionWithNoArguments()
        {
            var boundObjectCreationExpression = TestUtils.BindExpression<BoundObjectCreationExpression>("new System.Exception()");

            var exceptionType = TestDefaults.DefaultClrTypeCache.Resolve(typeof(Exception).FullName);
            boundObjectCreationExpression.ResultType.Should().Be(exceptionType);
            boundObjectCreationExpression.ConstructorInfo.Should().NotBeNull();
            boundObjectCreationExpression.BoundArguments.Should().BeEmpty();
        }

        [Theory]
        [InlineData("new System.Exception(\"exception message\")")]
        [InlineData("new System.Exception(message: \"exception message\")")]
        public void TestBoundObjectCreationExpressionWithOneArgument(string inputText)
        {
            var boundObjectCreationExpression = TestUtils.BindExpression<BoundObjectCreationExpression>(inputText);

            var exceptionType = TestDefaults.DefaultClrTypeCache.Resolve(typeof(Exception).FullName);
            boundObjectCreationExpression.ResultType.Should().Be(exceptionType);
            boundObjectCreationExpression.ConstructorInfo.Should().NotBeNull();
            boundObjectCreationExpression.BoundArguments.Count.Should().Be(1);

            var message = boundObjectCreationExpression.BoundArguments[0].As<BoundConstant>();
            message.Value.Should().Be("exception message");
        }
    }
}
