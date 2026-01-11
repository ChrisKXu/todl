using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class NameExpressionTests
{
    #region Parser Tests

    [Theory]
    [InlineData("Console")]
    [InlineData("myVariable")]
    [InlineData("myVar123")]
    public void TestSimpleNameExpression_Identifier(string identifier)
    {
        var name = TestUtils.ParseExpression<SimpleNameExpression>(identifier);

        name.Should().NotBeNull();
        name.IdentifierToken.Text.ToString().Should().Be(identifier);
        name.CanonicalName.Should().Be(identifier);
        name.GetUnqualifiedName().Should().BeSameAs(name);
    }

    [Theory]
    [InlineData("int", SyntaxKind.IntKeywordToken)]
    [InlineData("string", SyntaxKind.StringKeywordToken)]
    [InlineData("bool", SyntaxKind.BoolKeywordToken)]
    [InlineData("void", SyntaxKind.VoidKeywordToken)]
    [InlineData("byte", SyntaxKind.ByteKeywordToken)]
    [InlineData("char", SyntaxKind.CharKeywordToken)]
    [InlineData("long", SyntaxKind.LongKeywordToken)]
    public void TestSimpleNameExpression_BuiltInType(string typeName, SyntaxKind expectedKind)
    {
        var name = TestUtils.ParseExpression<SimpleNameExpression>(typeName);

        name.Should().NotBeNull();
        name.IdentifierToken.Kind.Should().Be(expectedKind);
        name.CanonicalName.Should().Be(typeName);
    }

    [Fact]
    public void TestNamespaceQualifiedNameExpression_SingleNamespace()
    {
        var name = TestUtils.ParseExpression<NamespaceQualifiedNameExpression>("System::Console");

        name.Should().NotBeNull();
        name.CanonicalName.Should().Be("System.Console");
        name.NamespaceIdentifiers.Should().HaveCount(1);
        name.NamespaceIdentifiers[0].Text.ToString().Should().Be("System");
        name.TypeIdentifierToken.Text.ToString().Should().Be("Console");
    }

    [Fact]
    public void TestNamespaceQualifiedNameExpression_TwoNamespaces()
    {
        var name = TestUtils.ParseExpression<NamespaceQualifiedNameExpression>("System::Threading::Task");

        name.Should().NotBeNull();
        name.CanonicalName.Should().Be("System.Threading.Task");
        name.NamespaceIdentifiers.Should().HaveCount(2);
        name.NamespaceIdentifiers[0].Text.ToString().Should().Be("System");
        name.NamespaceIdentifiers[1].Text.ToString().Should().Be("Threading");
        name.TypeIdentifierToken.Text.ToString().Should().Be("Task");
    }

    [Fact]
    public void TestNamespaceQualifiedNameExpression_ThreeNamespaces()
    {
        var name = TestUtils.ParseExpression<NamespaceQualifiedNameExpression>("System::Collections::Generic::List");

        name.Should().NotBeNull();
        name.CanonicalName.Should().Be("System.Collections.Generic.List");
        name.NamespaceIdentifiers.Should().HaveCount(3);
        name.NamespaceIdentifiers[0].Text.ToString().Should().Be("System");
        name.NamespaceIdentifiers[1].Text.ToString().Should().Be("Collections");
        name.NamespaceIdentifiers[2].Text.ToString().Should().Be("Generic");
        name.TypeIdentifierToken.Text.ToString().Should().Be("List");
    }

    [Fact]
    public void TestNamespaceQualifiedNameExpression_GetUnqualifiedName()
    {
        var name = TestUtils.ParseExpression<NamespaceQualifiedNameExpression>("System::Collections::Generic::Dictionary");

        var unqualified = name.GetUnqualifiedName();
        unqualified.Should().NotBeNull();
        unqualified.Should().BeOfType<SimpleNameExpression>();
        unqualified.CanonicalName.Should().Be("Dictionary");
        unqualified.IdentifierToken.Text.ToString().Should().Be("Dictionary");
    }

    [Fact]
    public void TestNamespaceQualifiedNameExpression_TextSpan()
    {
        var name = TestUtils.ParseExpression<NamespaceQualifiedNameExpression>("System::Console");

        name.Text.ToString().Should().Be("System::Console");
    }

    #endregion

    #region Binder Tests

    [Fact]
    public void TestSimpleNameExpression_ResolvesToVariable()
    {
        var inputText = @"
        {
            let x = 42;
            x;
        }
        ";
        var blockStatement = TestUtils.BindStatement<BoundBlockStatement>(inputText);

        blockStatement.Statements.Should().HaveCount(2);
        var expressionStatement = blockStatement.Statements[1].As<BoundExpressionStatement>();
        expressionStatement.Expression.Should().BeOfType<BoundVariableExpression>();

        var variableExpression = (BoundVariableExpression)expressionStatement.Expression;
        variableExpression.Variable.Name.Should().Be("x");
    }

    [Fact]
    public void TestNamespaceQualifiedNameExpression_ResolvesToType()
    {
        var boundExpression = TestUtils.BindExpression<BoundTypeExpression>("System::Console");

        boundExpression.Should().NotBeNull();
        boundExpression.ResultType.Should().NotBeNull();
        boundExpression.ResultType.Name.Should().Be("System.Console");
    }

    [Fact]
    public void TestNamespaceQualifiedNameExpression_NestedNamespace_ResolvesToType()
    {
        var boundExpression = TestUtils.BindExpression<BoundTypeExpression>("System::Threading::Tasks::Task");

        boundExpression.Should().NotBeNull();
        boundExpression.ResultType.Should().NotBeNull();
        boundExpression.ResultType.Name.Should().Be("System.Threading.Tasks.Task");
    }

    [Fact]
    public void TestNamespaceQualifiedNameExpression_InvalidType_ProducesError()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundTypeExpression>("System::NonExistentType", diagnosticBuilder);

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().NotBeEmpty();
        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.TypeNotFound);
    }

    [Fact]
    public void TestSimpleNameExpression_UndeclaredVariable_ProducesError()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundVariableExpression>("undeclaredVariable", diagnosticBuilder);

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().NotBeEmpty();
        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.UndeclaredVariable);
    }

    #endregion
}
