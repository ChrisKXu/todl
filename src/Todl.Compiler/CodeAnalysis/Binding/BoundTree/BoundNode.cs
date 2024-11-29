using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

internal abstract class BoundNode
{
    public SyntaxNode SyntaxNode { get; internal init; }

    public abstract BoundNode Accept(BoundTreeVisitor visitor);

}
