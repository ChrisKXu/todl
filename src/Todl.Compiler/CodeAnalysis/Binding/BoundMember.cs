using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundMember
    {
    }

    public sealed partial class Binder
    {
        public BoundMember BindMember(BoundScope scope, Member member)
        {
            return member switch
            {
                FunctionDeclarationMember functionDeclarationMember => BindFunctionDeclarationMember(scope, functionDeclarationMember),
                _ => null
            };
        }
    }
}
