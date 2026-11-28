using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

// Wrapping into System.Nullable<T> and null -> T? are the conversions currently produced by the Binder.
[BoundNode]
internal sealed class BoundConversionExpression : BoundExpression
{
    public BoundExpression Operand { get; internal init; }
    public ConversionKind ConversionKind { get; internal init; }

    public override bool Constant
        => ConversionKind is ConversionKind.NullLiteral || Operand.Constant;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundConversionExpression(this);
}
