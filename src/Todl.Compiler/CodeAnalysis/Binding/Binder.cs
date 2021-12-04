using System.Collections.Generic;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    /// <summary>
    /// Binder reorganizes SyntaxTree elements into Bound counterparts
    /// and prepares necessary information for Emitter to use in the emit process
    /// </summary>
    public sealed partial class Binder
    {
        private readonly BinderFlags binderFlags;
        private readonly BoundScope scope;

        internal Binder(
            BinderFlags binderFlags,
            BoundScope scope)
        {
            this.binderFlags = binderFlags;
            this.scope = scope;
        }
    }
}
