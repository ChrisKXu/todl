﻿using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundObjectCreationExpression : BoundExpression
{
    public ConstructorInfo ConstructorInfo { get; internal init; }
    public ImmutableArray<BoundExpression> BoundArguments { get; internal init; }
    public override TypeSymbol ResultType
        => SyntaxNode.SyntaxTree.ClrTypeCache.Resolve(ConstructorInfo.DeclaringType);

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundObjectCreationExpression(this);
}

public partial class Binder
{
    private BoundObjectCreationExpression BindNewExpression(NewExpression newExpression)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundTypeExpression = BindTypeExpression(newExpression.TypeNameExpression);
        diagnosticBuilder.Add(boundTypeExpression);

        // Treating no arguments as the same way of positional arguments
        if (newExpression.Arguments.Items.IsEmpty || !newExpression.Arguments.Items[0].IsNamedArgument)
        {
            return BindNewExpressionWithPositionalArgumentsInternal(
                diagnosticBuilder: diagnosticBuilder,
                targetType: boundTypeExpression.ResultType,
                newExpression: newExpression);
        }

        return BindNewExpressionWithNamedArgumentsInternal(
            diagnosticBuilder: diagnosticBuilder,
            targetType: boundTypeExpression.ResultType,
            newExpression: newExpression);
    }

    private BoundObjectCreationExpression BindNewExpressionWithPositionalArgumentsInternal(
        DiagnosticBag.Builder diagnosticBuilder,
        TypeSymbol targetType,
        NewExpression newExpression)
    {
        Debug.Assert(targetType.IsNative);

        var clrType = (targetType as ClrTypeSymbol).ClrType;
        var boundArguments = newExpression.Arguments.Items.Select(a => BindExpression(a.Expression));
        var argumentTypes = boundArguments.Select(b => (b.ResultType as ClrTypeSymbol).ClrType).ToArray();

        var constructorInfo = clrType.GetConstructor(argumentTypes);

        if (constructorInfo is null)
        {
            ReportNoMatchingConstructorCandidate(diagnosticBuilder, newExpression);
        }

        diagnosticBuilder.AddRange(boundArguments);

        return new()
        {
            SyntaxNode = newExpression,
            ConstructorInfo = constructorInfo,
            BoundArguments = boundArguments.ToImmutableArray(),
            DiagnosticBuilder = diagnosticBuilder
        };
    }

    private BoundObjectCreationExpression BindNewExpressionWithNamedArgumentsInternal(
        DiagnosticBag.Builder diagnosticBuilder,
        TypeSymbol targetType,
        NewExpression newExpression)
    {
        Debug.Assert(targetType.IsNative);

        var clrType = (targetType as ClrTypeSymbol).ClrType;
        var arguments = newExpression.Arguments;
        var candidates = clrType.GetConstructors()
            .Where(c => c.IsPublic && c.GetParameters().Length == arguments.Items.Length);
        var argumentsDictionary = arguments.Items.ToDictionary(
            keySelector: a => a.Identifier.Value.Text.ToString(),
            elementSelector: a => BindExpression(a.Expression));
        var nameAndTypes = argumentsDictionary.Select(a => new Tuple<string, Type>(a.Key, ((ClrTypeSymbol)a.Value.ResultType).ClrType)).ToHashSet();

        var constructorInfo = candidates.FirstOrDefault(c =>
        {
            var parameters = c.GetParameters().Select(p => new Tuple<string, Type>(p.Name, p.ParameterType));
            return nameAndTypes.SetEquals(parameters);
        });

        if (constructorInfo is null)
        {
            ReportNoMatchingConstructorCandidate(diagnosticBuilder, newExpression);

            return new()
            {
                SyntaxNode = newExpression,
                DiagnosticBuilder = diagnosticBuilder
            };
        }

        var boundArguments = constructorInfo.GetParameters().OrderBy(p => p.Position).Select(p => argumentsDictionary[p.Name]).ToList();
        diagnosticBuilder.AddRange(boundArguments);

        return new()
        {
            SyntaxNode = newExpression,
            ConstructorInfo = constructorInfo,
            BoundArguments = boundArguments.ToImmutableArray(),
            DiagnosticBuilder = diagnosticBuilder
        };
    }

    private void ReportNoMatchingConstructorCandidate(
        DiagnosticBag.Builder diagnosticBuilder,
        NewExpression newExpression)
    {
        diagnosticBuilder.Add(
            new Diagnostic()
            {
                Message = $"No matching constructor {newExpression.TypeNameExpression.Text} found.",
                Level = DiagnosticLevel.Error,
                TextLocation = newExpression.TypeNameExpression.Text.GetTextLocation(),
                ErrorCode = ErrorCode.NoMatchingCandidate
            });
    }
}
