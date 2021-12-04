using System;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundMember : BoundNode { }

    public sealed partial class Binder
    {
        public BoundMember BindMember(Member member)
            => member switch
            {
                FunctionDeclarationMember functionDeclarationMember => BindFunctionDeclarationMember(functionDeclarationMember),
                _ => throw new NotSupportedException()
            };
    }
}
