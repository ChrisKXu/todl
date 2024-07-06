using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundClrFunctionCallExpression : BoundExpression
{
    public BoundExpression BoundBaseExpression { get; internal init; }
    public MethodInfo MethodInfo { get; internal init; }
    public IReadOnlyList<BoundExpression> BoundArguments { get; internal init; }
    public override TypeSymbol ResultType => BoundBaseExpression.SyntaxNode.SyntaxTree.ClrTypeCache.Resolve(MethodInfo.ReturnType);
    public bool IsStatic => MethodInfo.IsStatic;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundClrFunctionCallExpression(this);
}

public partial class Binder
{
    private BoundExpression BindFunctionCallExpression(FunctionCallExpression functionCallExpression)
    {
        if (functionCallExpression.BaseExpression is not null)
        {
            return BindClrFunctionCallExpression(functionCallExpression);
        }

        return BindTodlFunctionCallExpression(functionCallExpression);
    }

    private BoundClrFunctionCallExpression BindClrFunctionCallExpression(FunctionCallExpression functionCallExpression)
    {
        var boundBaseExpression = BindExpression(functionCallExpression.BaseExpression);

        // Since all or none of the arguments of a FunctionCallExpression needs to be named,
        // we only need to check the first argument to see if it's a named argument to determine the others
        if (functionCallExpression.Arguments.Items.Any(a => a.IsNamedArgument))
        {
            return BindFunctionCallWithNamedArgumentsInternal(
                boundBaseExpression: boundBaseExpression,
                functionCallExpression: functionCallExpression);
        }

        return BindFunctionCallWithPositionalArgumentsInternal(
            boundBaseExpression: boundBaseExpression,
            functionCallExpression: functionCallExpression);
    }

    private BoundClrFunctionCallExpression BindFunctionCallWithNamedArgumentsInternal(
        BoundExpression boundBaseExpression,
        FunctionCallExpression functionCallExpression)
    {
        Debug.Assert(boundBaseExpression.ResultType.IsNative);

        var diagnosticBuilder = new DiagnosticBag.Builder();

        var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
        var isStatic = boundBaseExpression is BoundTypeExpression;
        var candidates = type
            .GetMethods()
            .Where(m => m.Name == functionCallExpression.NameToken.Text.ToString()
                && m.IsStatic == isStatic
                && !m.ContainsGenericParameters
                && m.IsPublic
                && m.GetParameters().Length == functionCallExpression.Arguments.Items.Count);

        var arguments = functionCallExpression.Arguments.Items.ToDictionary(
            keySelector: a => a.Identifier.Value.Text.ToString(),
            elementSelector: a => BindExpression(a.Expression));

        var nameAndTypes = arguments.Select(a => new Tuple<string, Type>(a.Key, ((ClrTypeSymbol)a.Value.ResultType).ClrType)).ToHashSet();

        var candidate = candidates.FirstOrDefault(methodInfo =>
        {
            var parameters = methodInfo.GetParameters().Select(p => new Tuple<string, Type>(p.Name, p.ParameterType));
            return nameAndTypes.SetEquals(parameters);
        });

        if (candidate is null)
        {
            ReportNoMatchingFunctionCandidate(diagnosticBuilder, functionCallExpression);
        }

        var boundArguments = candidate?.GetParameters().OrderBy(p => p.Position).Select(p => arguments[p.Name]);

        return BoundNodeFactory.CreateBoundClrFunctionCallExpression(
            syntaxNode: functionCallExpression,
            boundBaseExpression: boundBaseExpression,
            methodInfo: candidate,
            boundArguments: boundArguments.ToList(),
            diagnosticBuilder: diagnosticBuilder);
    }

    private BoundClrFunctionCallExpression BindFunctionCallWithPositionalArgumentsInternal(
        BoundExpression boundBaseExpression,
        FunctionCallExpression functionCallExpression)
    {
        Debug.Assert(boundBaseExpression.ResultType.IsNative);

        var diagnosticBuilder = new DiagnosticBag.Builder();

        var boundArguments = functionCallExpression.Arguments.Items.Select(a => BindExpression(a.Expression));
        var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;

        var argumentTypes = boundArguments.Select(b => (b.ResultType as ClrTypeSymbol).ClrType).ToArray();

        var candidate = type.GetMethod(
            name: functionCallExpression.NameToken.Text.ToString(),
            genericParameterCount: 0,
            types: argumentTypes);

        if (candidate is null)
        {
            ReportNoMatchingFunctionCandidate(diagnosticBuilder, functionCallExpression);
        }

        return BoundNodeFactory.CreateBoundClrFunctionCallExpression(
            syntaxNode: functionCallExpression,
            boundBaseExpression: boundBaseExpression,
            methodInfo: candidate,
            boundArguments: boundArguments.ToList(),
            diagnosticBuilder: diagnosticBuilder);
    }

    private void ReportNoMatchingFunctionCandidate(
        DiagnosticBag.Builder diagnosticBuilder,
        FunctionCallExpression functionCallExpression)
    {
        diagnosticBuilder.Add(
            new Diagnostic()
            {
                Message = $"No matching function '{functionCallExpression.NameToken.Text}' found.",
                Level = DiagnosticLevel.Error,
                TextLocation = functionCallExpression.NameToken.Text.GetTextLocation(),
                ErrorCode = ErrorCode.NoMatchingCandidate
            });
    }
}
