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
    }

    public sealed partial class Binder
    {
        internal BoundExpression BindExpression(Expression expression)
        {
            return expression switch
            {
                LiteralExpression literalExpression => this.BindLiteralExpression(literalExpression),
                BinaryExpression binaryExpression => this.BindBinaryExpression(binaryExpression),
                UnaryExpression unaryExpression => this.BindUnaryExpression(unaryExpression),
                ParethesizedExpression parethesizedExpression => this.BindExpression(parethesizedExpression.InnerExpression),
                AssignmentExpression assignmentExpression => this.BindAssignmentExpression(assignmentExpression),
                NameExpression nameExpression => this.BindNameExpression(nameExpression),
                _ => throw new NotImplementedException()
            };
        }
    }
}
