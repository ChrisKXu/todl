using System;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundMember : BoundNode { }

    public partial class Binder
    {
        public BoundMember BindMember(Member member)
            => member switch
            {
                FunctionDeclarationMember functionDeclarationMember => BindFunctionDeclarationMember(functionDeclarationMember),
                VariableDeclarationMember variableDeclarationMember => BindVariableDeclarationMember(variableDeclarationMember),
                _ => throw new NotSupportedException()
            };
    }
}
