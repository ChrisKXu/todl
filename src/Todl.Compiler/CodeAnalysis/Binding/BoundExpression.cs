using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundExpression
    {
        public virtual TypeSymbol ResultType { get; internal init; }
        public virtual bool LValue => false;
    }

    public sealed partial class Binder
    {
        internal BoundExpression BindExpression(BoundScope scope, Expression expression)
        {
            return expression switch
            {
                LiteralExpression literalExpression => BindLiteralExpression(literalExpression),
                BinaryExpression binaryExpression => BindBinaryExpression(scope, binaryExpression),
                UnaryExpression unaryExpression => BindUnaryExpression(scope, unaryExpression),
                ParethesizedExpression parethesizedExpression => BindExpression(scope, parethesizedExpression.InnerExpression),
                AssignmentExpression assignmentExpression => BindAssignmentExpression(scope, assignmentExpression),
                NameExpression nameExpression => BindNameExpression(scope, nameExpression),
                MemberAccessExpression memberAccessExpression => BindMemberAccessExpression(scope, memberAccessExpression),
                FunctionCallExpression functionCallExpression => BindFunctionCallExpression(scope, functionCallExpression),
                _ => ReportErrorExpression(new Diagnostic(
                    message: $"Unsupported expression type",
                    level: DiagnosticLevel.Error,
                    textLocation: default))
            };
        }
    }
}
