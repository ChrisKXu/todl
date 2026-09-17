using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

// Not yet produced by any Binder call site; groundwork for wiring implicit conversions in.
[BoundNode]
internal sealed class BoundConversionExpression : BoundExpression
{
    public BoundExpression Operand { get; internal init; }
    public TypeSymbol TargetType { get; internal init; }
    public ConversionKind ConversionKind { get; internal init; }

    public override TypeSymbol ResultType => TargetType;
    public override bool Constant => Operand.Constant;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundConversionExpression(this);
}
