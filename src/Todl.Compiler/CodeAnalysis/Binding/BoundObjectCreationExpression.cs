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
        private BoundExpression BindNewExpression(BoundScope scope, NewExpression newExpression)
        {
            var boundTypeExpression = BindNameExpression(scope, newExpression.TypeNameExpression);
            if (boundTypeExpression is not BoundTypeExpression)
            {
                return ReportErrorExpression(new Diagnostic()
                {
                    Message = $"Type '{newExpression.TypeNameExpression.QualifiedName}' is invalid",
                    Level = DiagnosticLevel.Error,
                    ErrorCode = ErrorCode.TypeNotFound,
                    TextLocation = newExpression.NewKeywordToken.GetTextLocation()
                });
            }

            // Treating no arguments as the same way of positional arguments
            if (!newExpression.Arguments.Items.Any() || !newExpression.Arguments.Items[0].IsNamedArgument)
            {
                return BindNewExpressionWithPositionalArgumentsInternal(
                    scope: scope,
                    targetType: boundTypeExpression.ResultType,
                    arguments: newExpression.Arguments);
            }

            return BindNewExpressionWithNamedArgumentsInternal(
                scope: scope,
                targetType: boundTypeExpression.ResultType,
                arguments: newExpression.Arguments);
        }

        private BoundExpression BindNewExpressionWithPositionalArgumentsInternal(
            BoundScope scope,
            TypeSymbol targetType,
            CommaSeparatedSyntaxList<Argument> arguments)
        {
            Debug.Assert(targetType.IsNative);

            var clrType = (targetType as ClrTypeSymbol).ClrType;
            var boundArguments = arguments.Items.Select(a => BindExpression(scope, a.Expression));
            var argumentTypes = boundArguments.Select(b => (b.ResultType as ClrTypeSymbol).ClrType).ToArray();

            var constructorInfo = clrType.GetConstructor(argumentTypes);

            if (constructorInfo is null)
            {
                return ReportNoMatchingCandidate();
            }

            return new BoundObjectCreationExpression()
            {
                ConstructorInfo = constructorInfo,
                ResultType = targetType,
                BoundArguments = boundArguments.ToList()
            };
        }

        private BoundExpression BindNewExpressionWithNamedArgumentsInternal(
            BoundScope scope,
            TypeSymbol targetType,
            CommaSeparatedSyntaxList<Argument> arguments)
        {
            Debug.Assert(targetType.IsNative);

            var clrType = (targetType as ClrTypeSymbol).ClrType;
            var candidates = clrType.GetConstructors()
                .Where(c => c.IsPublic && c.GetParameters().Length == arguments.Items.Count);
            var argumentsDictionary = arguments.Items.ToDictionary(
                keySelector: a => a.Identifier.Text.ToString(),
                elementSelector: a => BindExpression(scope, a.Expression));
            var nameAndTypes = argumentsDictionary.Select(a => new Tuple<string, Type>(a.Key, ((ClrTypeSymbol)a.Value.ResultType).ClrType)).ToHashSet();

            var constructorInfo = candidates.FirstOrDefault(c =>
            {
                var parameters = c.GetParameters().Select(p => new Tuple<string, Type>(p.Name, p.ParameterType));
                return nameAndTypes.SetEquals(parameters);
            });

            if (constructorInfo is null)
            {
                return ReportNoMatchingCandidate();
            }

            var boundArguments = constructorInfo.GetParameters().OrderBy(p => p.Position).Select(p => argumentsDictionary[p.Name]).ToList();

            return new BoundObjectCreationExpression()
            {
                ConstructorInfo = constructorInfo,
                ResultType = targetType,
                BoundArguments = boundArguments.ToList()
            };
        }
    }
}
