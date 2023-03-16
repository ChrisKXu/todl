using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.Tests;

internal static class TestUtils
{
    internal static TBoundExpression BindExpression<TBoundExpression>(
            string inputText)
            where TBoundExpression : BoundExpression
    {
        var expression = SyntaxTree.ParseExpression(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache);
        return binder.BindExpression(expression).As<TBoundExpression>();
    }

    internal static TBoundStatement BindStatement<TBoundStatement>(
        string inputText)
        where TBoundStatement : BoundStatement
    {
        var statement = SyntaxTree.ParseStatement(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache);
        return binder.BindStatement(statement) as TBoundStatement;
    }

    internal static TBoundMember BindMember<TBoundMember>(
        string inputText)
        where TBoundMember : BoundMember
    {
        var syntaxTree = ParseSyntaxTree(inputText);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache);
        var member = syntaxTree.Members[0];

        if (member is FunctionDeclarationMember functionDeclarationMember)
        {
            binder.Scope.DeclareFunction(FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember));
        }

        return binder.BindMember(member).As<TBoundMember>();
    }

    internal static SyntaxTree ParseSyntaxTree(string inputText)
        => SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
}
