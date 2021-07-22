namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundNamespaceExpression : BoundExpression
    {
        public string Namespace { get; internal init; }
    }
}
