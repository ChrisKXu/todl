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
            if (!newExpression.Arguments.Any() || !newExpression.Arguments[0].IsNamedArgument)
            {
                return BindNewExpressionWithPositionalArgumentsInternal(
                    scope: scope,
                    targetType: boundTypeExpression.ResultType,
                    arguments: newExpression.Arguments
                );
            }

            return new BoundObjectCreationExpression()
            {
                ResultType = boundTypeExpression.ResultType
            };
        }

        private BoundExpression BindNewExpressionWithPositionalArgumentsInternal(
            BoundScope scope,
            TypeSymbol targetType,
            ArgumentsList arguments)
        {
            Debug.Assert(targetType.IsNative);

            var clrType = (targetType as ClrTypeSymbol).ClrType;
            var boundArguments = arguments.Select(a => BindExpression(scope, a.Expression));
            var argumentTypes = boundArguments.Select(b => (b.ResultType as ClrTypeSymbol).ClrType).ToArray();

            var constructorInfo = clrType.GetConstructor(argumentTypes);

            if (constructorInfo is null)
            {
                ReportNoMatchingCandidate();
            }

            return new BoundObjectCreationExpression()
            {
                ConstructorInfo = constructorInfo,
                ResultType = targetType,
                BoundArguments = boundArguments.ToList()
            };
        }
    }
}
