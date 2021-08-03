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

                var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
                var methodInfos = type
                    .GetMethods()
                    .Where(m => m.Name == boundMemberAccessExpression.MemberName.Text.ToString()
                        && m.IsStatic == boundMemberAccessExpression.IsStatic
                        && !m.ContainsGenericParameters
                        && m.IsPublic
                        && m.GetParameters().Length == 0)
                    .ToList();

                if (!methodInfos.Any())
                {
                    return ReportErrorExpression(
                        new Diagnostic(
                            message: $"No matching function found.",
                            level: DiagnosticLevel.Error,
                            textLocation: default));
                }

                if (methodInfos.Count > 1)
                {
                    return ReportErrorExpression(
                        new Diagnostic(
                            message: $"Ambiguous function call.",
                            level: DiagnosticLevel.Error,
                            textLocation: default));
                }

                return new BoundFunctionCallExpression()
                {
                    BoundBaseExpression = boundBaseExpression,
                    MethodInfo = methodInfos[0]
                };
            }

            return ReportErrorExpression(new Diagnostic(
                message: $"Unsupported bound expression type",
                level: DiagnosticLevel.Error,
                textLocation: default));
        }
    }
}
