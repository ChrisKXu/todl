using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundExpression
    {
        public abstract TypeSymbol ResultType { get; }
        public virtual bool LValue => false;
    }

    public sealed partial class Binder
    {
        internal BoundExpression BindExpression(BoundScope scope, Expression expression)
        {
            return expression switch
            {
                LiteralExpression literalExpression => this.BindLiteralExpression(literalExpression),
                BinaryExpression binaryExpression => this.BindBinaryExpression(scope, binaryExpression),
                UnaryExpression unaryExpression => this.BindUnaryExpression(scope, unaryExpression),
                ParethesizedExpression parethesizedExpression => this.BindExpression(scope, parethesizedExpression.InnerExpression),
                AssignmentExpression assignmentExpression => this.BindAssignmentExpression(scope, assignmentExpression),
                NameExpression nameExpression => this.BindNameExpression(scope, nameExpression),
                _ => throw new NotImplementedException()
            };
        }
    }
}
