﻿using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundExpression : BoundNode
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
                NewExpression newExpression => BindNewExpression(scope, newExpression),
                _ => new BoundErrorExpression()
            };
        }
    }
}
