using System;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

internal abstract class BoundMember : BoundNode { }

public partial class Binder
{
    internal BoundMember BindMember(Member member)
        => member switch
        {
            FunctionDeclarationMember functionDeclarationMember => BindFunctionDeclarationMember(functionDeclarationMember),
            VariableDeclarationMember variableDeclarationMember => BindVariableDeclarationMember(variableDeclarationMember),
            _ => throw new NotSupportedException()
        };
}
