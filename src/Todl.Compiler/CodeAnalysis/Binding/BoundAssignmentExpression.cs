using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundAssignmentExpression : BoundExpression
    {
        public SyntaxToken VariableName { get; }
        public BoundExpression BoundExpression { get; }
        public override TypeSymbol ResultType => this.BoundExpression.ResultType;

        public BoundAssignmentExpression(SyntaxToken variableName, BoundExpression boundExpression)
        {
            this.VariableName = variableName;
            this.BoundExpression = boundExpression;
        }
    }

    public sealed partial class Binder
    {
        public BoundAssignmentExpression BindAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            return new BoundAssignmentExpression(
                variableName: assignmentExpression.IdentifierToken,
                boundExpression: this.BindExpression(assignmentExpression.Expression));
        }
    }
}
