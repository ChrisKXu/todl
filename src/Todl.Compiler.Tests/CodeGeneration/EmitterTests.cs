using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed partial class EmitterTests
{
    private static TBoundExpression BindExpression<TBoundExpression>(
            string inputText)
            where TBoundExpression : BoundExpression
    {
        var expression = SyntaxTree.ParseExpression(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache);
        return binder.BindExpression(expression).As<TBoundExpression>();
    }
}
