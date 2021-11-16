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
    public sealed class BoundFunctionCallExpression : BoundExpression
    {
        public BoundExpression BoundBaseExpression { get; internal init; }
        public MethodInfo MethodInfo { get; internal init; }
        public IReadOnlyList<BoundExpression> BoundArguments { get; internal init; }
        public override TypeSymbol ResultType { get => ClrTypeSymbol.MapClrType(MethodInfo.ReturnType); }
        public bool IsStatic => MethodInfo.IsStatic;
    }

    public sealed partial class Binder
    {
        private BoundExpression BindFunctionCallExpression(BoundScope scope, FunctionCallExpression functionCallExpression)
        {
            var boundBaseExpression = BindExpression(scope, functionCallExpression.BaseExpression);

            // Since all or none of the arguments of a FunctionCallExpression needs to be named,
            // we only need to check the first argument to see if it's a named argument to determine the others
            if (functionCallExpression.Arguments.Items.Any() && functionCallExpression.Arguments.Items[0].IsNamedArgument)
            {
                return BindFunctionCallWithNamedArgumentsInternal(
                    scope: scope,
                    boundBaseExpression: boundBaseExpression,
                    functionCallExpression: functionCallExpression);
            }

            return BindFunctionCallWithPositionalArgumentsInternal(
                scope: scope,
                boundBaseExpression: boundBaseExpression,
                functionCallExpression: functionCallExpression);
        }

        private BoundExpression BindFunctionCallWithNamedArgumentsInternal(
            BoundScope scope,
            BoundExpression boundBaseExpression,
            FunctionCallExpression functionCallExpression)
        {
            Debug.Assert(boundBaseExpression.ResultType.IsNative);

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
                elementSelector: a => BindExpression(scope, a.Expression));

            var nameAndTypes = arguments.Select(a => new Tuple<string, Type>(a.Key, ((ClrTypeSymbol)a.Value.ResultType).ClrType)).ToHashSet();

            var candidate = candidates.FirstOrDefault(methodInfo =>
            {
                var parameters = methodInfo.GetParameters().Select(p => new Tuple<string, Type>(p.Name, p.ParameterType));
                return nameAndTypes.SetEquals(parameters);
            });

            if (candidate is null)
            {
                return ReportNoMatchingCandidate();
            }

            var boundArguments = candidate.GetParameters().OrderBy(p => p.Position).Select(p => arguments[p.Name]).ToList();

            return new BoundFunctionCallExpression()
            {
                BoundBaseExpression = boundBaseExpression,
                MethodInfo = candidate,
                BoundArguments = boundArguments
            };
        }

        private BoundExpression BindFunctionCallWithPositionalArgumentsInternal(
            BoundScope scope,
            BoundExpression boundBaseExpression,
            FunctionCallExpression functionCallExpression)
        {
            Debug.Assert(boundBaseExpression.ResultType.IsNative);

            var boundArguments = functionCallExpression.Arguments.Items.Select(a => BindExpression(scope, a.Expression));
            var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;

            var argumentTypes = boundArguments.Select(b => (b.ResultType as ClrTypeSymbol).ClrType).ToArray();

            var candidate = type.GetMethod(
                name: functionCallExpression.NameToken.Text.ToString(),
                genericParameterCount: 0,
                types: argumentTypes);

            if (candidate is null)
            {
                return ReportNoMatchingCandidate();
            }

            return new BoundFunctionCallExpression()
            {
                BoundBaseExpression = boundBaseExpression,
                MethodInfo = candidate,
                BoundArguments = boundArguments.ToList()
            };
        }

        private BoundErrorExpression ReportNoMatchingCandidate()
        {
            return ReportErrorExpression(
                new Diagnostic()
                {
                    Message = $"No matching function found.",
                    Level = DiagnosticLevel.Error,
                    TextLocation = default,
                    ErrorCode = ErrorCode.NoMatchingCandidate
                });
        }
    }
}
