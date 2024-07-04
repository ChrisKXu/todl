using System;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Symbols;

// only used in repl to create on-the-fly variables
public sealed class ReplVariableSymbol : VariableSymbol
{
    public AssignmentExpression AssignmentExpression { get; internal init; }
    public BoundExpression BoundInitializer { get; internal init; }

    public override string Name => AssignmentExpression.Left.Text.ToString();
    public override bool ReadOnly => false;
    public override TypeSymbol Type => BoundInitializer.ResultType;

    public override bool Equals(Symbol other)
        => other is ReplVariableSymbol replVariableSymbol
        && replVariableSymbol.AssignmentExpression.Equals(AssignmentExpression);

    public override int GetHashCode()
        => HashCode.Combine(AssignmentExpression);
}
