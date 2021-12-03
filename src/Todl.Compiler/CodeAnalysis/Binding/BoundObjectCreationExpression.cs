using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundObjectCreationExpression : BoundExpression
    {
        public ConstructorInfo ConstructorInfo { get; internal init; }
        public IReadOnlyList<BoundExpression> BoundArguments { get; internal init; }
    }

    public sealed partial class Binder
    {
        private BoundObjectCreationExpression BindNewExpression(BoundScope scope, NewExpression newExpression)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var boundTypeExpression = BindNameExpression(scope, newExpression.TypeNameExpression);
            diagnosticBuilder.Add(boundTypeExpression);

            if (boundTypeExpression is not BoundTypeExpression)
            {
                diagnosticBuilder.Add(new Diagnostic()
                {
                    Message = $"Type '{newExpression.TypeNameExpression.Text}' is invalid",
                    Level = DiagnosticLevel.Error,
                    ErrorCode = ErrorCode.TypeNotFound,
                    TextLocation = newExpression.TypeNameExpression.Text.GetTextLocation()
                });

                return new()
                {
                    SyntaxNode = newExpression,
                    DiagnosticBuilder = diagnosticBuilder
                };
            }

            // Treating no arguments as the same way of positional arguments
            if (!newExpression.Arguments.Items.Any() || !newExpression.Arguments.Items[0].IsNamedArgument)
            {
                return BindNewExpressionWithPositionalArgumentsInternal(
                    scope: scope,
                    diagnosticBuilder: diagnosticBuilder,
                    targetType: boundTypeExpression.ResultType,
                    newExpression: newExpression);
            }

            return BindNewExpressionWithNamedArgumentsInternal(
                scope: scope,
                diagnosticBuilder: diagnosticBuilder,
                targetType: boundTypeExpression.ResultType,
                newExpression: newExpression);
        }

        private BoundObjectCreationExpression BindNewExpressionWithPositionalArgumentsInternal(
            BoundScope scope,
            DiagnosticBag.Builder diagnosticBuilder,
            TypeSymbol targetType,
            NewExpression newExpression)
        {
            Debug.Assert(targetType.IsNative);

            var clrType = (targetType as ClrTypeSymbol).ClrType;
            var boundArguments = newExpression.Arguments.Items.Select(a => BindExpression(scope, a.Expression));
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
                ResultType = targetType,
                BoundArguments = boundArguments.ToList(),
                DiagnosticBuilder = diagnosticBuilder
            };
        }

        private BoundObjectCreationExpression BindNewExpressionWithNamedArgumentsInternal(
            BoundScope scope,
            DiagnosticBag.Builder diagnosticBuilder,
            TypeSymbol targetType,
            NewExpression newExpression)
        {
            Debug.Assert(targetType.IsNative);

            var clrType = (targetType as ClrTypeSymbol).ClrType;
            var arguments = newExpression.Arguments;
            var candidates = clrType.GetConstructors()
                .Where(c => c.IsPublic && c.GetParameters().Length == arguments.Items.Count);
            var argumentsDictionary = arguments.Items.ToDictionary(
                keySelector: a => a.Identifier.Value.Text.ToString(),
                elementSelector: a => BindExpression(scope, a.Expression));
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
                ResultType = targetType,
                BoundArguments = boundArguments.ToList(),
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
}
