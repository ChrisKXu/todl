using System;
using System.Collections.Generic;
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
        public IReadOnlyDictionary<string, BoundExpression> BoundArguments { get; internal init; }
        public override TypeSymbol ResultType { get => ClrTypeSymbol.MapClrType(MethodInfo.ReturnType); }
    }

    public sealed partial class Binder
    {
        private BoundExpression BindFunctionCallExpression(BoundScope scope, FunctionCallExpression functionCallExpression)
        {
            var boundBaseExpression = BindExpression(scope, functionCallExpression.BaseExpression);

            if (!boundBaseExpression.ResultType.IsNative)
            {
                return ReportErrorExpression(
                    new Diagnostic(
                        message: $"Type {boundBaseExpression.ResultType} is not supported.",
                        level: DiagnosticLevel.Error,
                        textLocation: default));
            }

            if (boundBaseExpression is BoundMemberAccessExpression boundMemberAccessExpression)
            {
                if (boundMemberAccessExpression.BoundMemberAccessKind != BoundMemberAccessKind.Function)
                {
                    return ReportErrorExpression(new Diagnostic(
                        message: $"Member {boundMemberAccessExpression.MemberName} is not a function",
                        level: DiagnosticLevel.Error,
                        textLocation: default));
                }

                var type = (boundMemberAccessExpression.BoundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
                var candidates = type
                    .GetMethods()
                    .Where(m => m.Name == boundMemberAccessExpression.MemberName.Text.ToString()
                        && m.IsStatic == boundMemberAccessExpression.IsStatic
                        && !m.ContainsGenericParameters
                        && m.IsPublic
                        && m.GetParameters().Length == functionCallExpression.Arguments.Count);

                if (!functionCallExpression.Arguments.Any())
                {
                    return BindFunctionCallWithNoArgumentsInternal(
                        boundBaseExpression: boundBaseExpression,
                        candidates: candidates,
                        functionCallExpression: functionCallExpression);
                }

                // Since all or none of the arguments of a FunctionCallExpression needs to be named,
                // we only need to check the first argument to see if it's a named argument to determine the others
                if (functionCallExpression.Arguments.First().IsNamedArgument)
                {
                    return BindFunctionCallWithNamedArgumentsInternal(
                        scope: scope,
                        boundBaseExpression: boundBaseExpression,
                        candidates: candidates,
                        functionCallExpression: functionCallExpression);
                }

                return BindFunctionCallWithPositionalArgumentsInternal(
                    scope: scope,
                    boundBaseExpression: boundBaseExpression,
                    candidates: candidates,
                    functionCallExpression: functionCallExpression);
            }

            return ReportErrorExpression(new Diagnostic(
                message: $"Unsupported bound expression type",
                level: DiagnosticLevel.Error,
                textLocation: default));
        }

        private BoundExpression BindFunctionCallWithNoArgumentsInternal(
            BoundExpression boundBaseExpression,
            IEnumerable<MethodInfo> candidates,
            FunctionCallExpression functionCallExpression)
        {
            var candidate = candidates.FirstOrDefault();

            if (candidate == default)
            {
                return ReportNoMatchingCandidate();
            }

            return new BoundFunctionCallExpression()
            {
                BoundBaseExpression = boundBaseExpression,
                MethodInfo = candidate,
                BoundArguments = new Dictionary<string, BoundExpression>()
            };
        }

        private BoundExpression BindFunctionCallWithNamedArgumentsInternal(
            BoundScope scope,
            BoundExpression boundBaseExpression,
            IEnumerable<MethodInfo> candidates,
            FunctionCallExpression functionCallExpression)
        {
            var boundArguments = functionCallExpression.Arguments.ToDictionary(
                keySelector: a => a.Identifier.Text.ToString(),
                elementSelector: a => BindExpression(scope, a.Expression));

            var namedArguments = boundArguments.Select(a => new Tuple<string, Type>(a.Key, ((ClrTypeSymbol)a.Value.ResultType).ClrType)).ToHashSet();

            var candidate = candidates.FirstOrDefault(methodInfo =>
            {
                var namedParameters = methodInfo.GetParameters().Select(p => new Tuple<string, Type>(p.Name, p.ParameterType));
                return namedArguments.SetEquals(namedParameters);
            });

            if (candidate == default)
            {
                return ReportNoMatchingCandidate();
            }

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
            IEnumerable<MethodInfo> candidates,
            FunctionCallExpression functionCallExpression)
        {
            var boundArguments = functionCallExpression.Arguments.Select(a => BindExpression(scope, a.Expression)).ToList();

            var candidate = candidates.FirstOrDefault(methodInfo =>
            {
                return Array.TrueForAll(methodInfo.GetParameters(), p => p.ParameterType == ((ClrTypeSymbol)boundArguments[p.Position].ResultType).ClrType);
            });

            if (candidate == default)
            {
                return ReportNoMatchingCandidate();
            }

            return ReportNoMatchingCandidate();
        }

        private BoundErrorExpression ReportNoMatchingCandidate()
        {
            return ReportErrorExpression(
                new Diagnostic(
                    message: $"No matching function found.",
                    level: DiagnosticLevel.Error,
                    textLocation: default));
        }
    }
}
