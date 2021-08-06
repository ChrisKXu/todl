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

        public static BoundImportDirective BindImportDirective(
            string inputText,
            Binder binder)
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(inputText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var importDirective = parser.ParseDirective().As<ImportDirective>();
            return binder.BindImportDirective(importDirective);
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
            var boundImportDirective = BindImportDirective(
                inputText: "import { Task } from System.Threading.Tasks;",
                binder: new Binder(BinderFlags.None));

            boundImportDirective.Namespace.Should().Be("System.Threading.Tasks");
            var taskType = boundImportDirective.ImportedTypes["Task"];
            taskType.Should().Be(typeof(System.Threading.Tasks.Task));
        }

        [Fact]
        public void TestBindImportDirectiveMultiple()
        {
            var boundImportDirective = BindImportDirective(
                inputText: "import { BinaryReader, BinaryWriter, FileStream } from System.IO;",
                binder: new Binder(BinderFlags.None));

            boundImportDirective.Namespace.Should().Be("System.IO");
            boundImportDirective.ImportedTypes.Count.Should().Be(3);
            boundImportDirective.ImportedTypes["BinaryReader"].Should().Be(typeof(System.IO.BinaryReader));
            boundImportDirective.ImportedTypes["BinaryWriter"].Should().Be(typeof(System.IO.BinaryWriter));
            boundImportDirective.ImportedTypes["FileStream"].Should().Be(typeof(System.IO.FileStream));
        }

        [Fact]
        public void TestBindImportDirectiveWildcard()
        {
            var boundImportDirective = BindImportDirective(
                inputText: "import * from System.Runtime.Loader;",
                binder: new Binder(BinderFlags.None));

            boundImportDirective.Namespace.Should().Be("System.Runtime.Loader");
            boundImportDirective.ImportedTypes.Count.Should().Be(2);
            boundImportDirective.ImportedTypes["AssemblyDependencyResolver"].Should().Be(typeof(System.Runtime.Loader.AssemblyDependencyResolver));
            boundImportDirective.ImportedTypes["AssemblyLoadContext"].Should().Be(typeof(System.Runtime.Loader.AssemblyLoadContext));
        }

        [Fact]
        public void TestBindMemberAccessExpressionInstanceProperty()
        {
            var boundMemberAccessExpression = BindExpression<BoundMemberAccessExpression>(
                inputText: "\"abc\".Length",
                binder: new Binder(BinderFlags.None),
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
                binder: new Binder(BinderFlags.None),
                scope: BoundScope.GlobalScope);

            boundMemberAccessExpression.BoundMemberAccessKind.Should().Be(BoundMemberAccessKind.Field);
            boundMemberAccessExpression.MemberName.Text.Should().Be("MaxValue");
            boundMemberAccessExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(int));
            boundMemberAccessExpression.IsStatic.Should().Be(true);
        }

        [Fact]
        public void TestBindFunctionCallExpression()
        {
            var boundFunctionCallExpression = BindExpression<BoundFunctionCallExpression>(
                inputText: "100.ToString()",
                binder: new Binder(BinderFlags.None),
                scope: BoundScope.GlobalScope);

            boundFunctionCallExpression.ResultType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(string));
            boundFunctionCallExpression.MethodInfo.Name.Should().Be("ToString");
        }
    }
}
