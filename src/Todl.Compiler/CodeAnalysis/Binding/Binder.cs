using System.Collections.Generic;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    /// <summary>
    /// Binder reorganizes SyntaxTree elements into Bound counterparts
    /// and prepares necessary information for Emitter to use in the emit process
    /// </summary>
    public partial class Binder
    {
        public BoundScope Scope { get; private init; }
        public Binder Parent { get; private init; }
        public virtual bool AllowVariableDeclarationInAssignment
            => Parent.AllowVariableDeclarationInAssignment;

        public Binder CreateBlockStatementBinder()
            => new()
            {
                Parent = this,
                Scope = Scope.CreateChildScope(BoundScopeKind.BlockStatement)
            };

        public Binder CreateFunctionBinder()
            => new()
            {
                Parent = this,
                Scope = Scope.CreateChildScope(BoundScopeKind.Function)
            };

        public static Binder CreateScriptBinder()
            => new ScriptBinder()
            {
                Scope = BoundScope.GlobalScope
            };

        public static Binder CreateModuleBinder()
            => new ModuleBinder()
            {
                Scope = BoundScope.GlobalScope.CreateChildScope(BoundScopeKind.Module)
            };

        internal sealed class ScriptBinder : Binder
        {
            public override bool AllowVariableDeclarationInAssignment => true;
        }

        internal sealed class ModuleBinder : Binder
        {
            public override bool AllowVariableDeclarationInAssignment => false;
        }
    }
}
