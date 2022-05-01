using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;
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

        public virtual BoundUnaryOperatorFactory BoundUnaryOperatorFactory
            => Parent?.BoundUnaryOperatorFactory;

        public virtual BoundBinaryOperatorFactory BoundBinaryOperatorFactory
            => Parent?.BoundBinaryOperatorFactory;

        public virtual ClrTypeCache ClrTypeCache
            => Parent?.ClrTypeCache;

        public virtual bool AllowVariableDeclarationInAssignment
            => Parent.AllowVariableDeclarationInAssignment;

        public virtual FunctionSymbol FunctionSymbol
            => Parent?.FunctionSymbol;

        public bool IsInFunction => FunctionSymbol is not null;

        public Binder CreateBlockStatementBinder()
            => new()
            {
                Parent = this,
                Scope = Scope.CreateChildScope(BoundScopeKind.BlockStatement)
            };

        public Binder CreateFunctionBinder(FunctionSymbol functionSymbol)
            => new FunctionBinder(functionSymbol)
            {
                Parent = this,
                Scope = Scope.CreateChildScope(BoundScopeKind.Function)
            };

        public static Binder CreateScriptBinder(ClrTypeCache clrTypeCache)
            => new ScriptBinder(clrTypeCache)
            {
                Scope = BoundScope.GlobalScope
            };

        public static Binder CreateModuleBinder(ClrTypeCache clrTypeCache)
            => new ModuleBinder(clrTypeCache)
            {
                Scope = BoundScope.GlobalScope.CreateChildScope(BoundScopeKind.Module)
            };

        public Binder CreateTypeBinder()
            => new TypeBinder()
            {
                Parent = this,
                Scope = Scope.CreateChildScope(BoundScopeKind.Type)
            };

        internal sealed class ScriptBinder : Binder
        {
            public ScriptBinder(ClrTypeCache clrTypeCache)
            {
                ClrTypeCache = clrTypeCache;
                BoundUnaryOperatorFactory = new(clrTypeCache);
                BoundBinaryOperatorFactory = new(clrTypeCache);
            }

            public override bool AllowVariableDeclarationInAssignment => true;
            public override ClrTypeCache ClrTypeCache { get; }
            public override BoundUnaryOperatorFactory BoundUnaryOperatorFactory { get; }
            public override BoundBinaryOperatorFactory BoundBinaryOperatorFactory { get; }
        }

        internal sealed class ModuleBinder : Binder
        {
            public ModuleBinder(ClrTypeCache clrTypeCache)
            {
                ClrTypeCache = clrTypeCache;
                BoundUnaryOperatorFactory = new(clrTypeCache);
                BoundBinaryOperatorFactory = new(clrTypeCache);
            }

            public override bool AllowVariableDeclarationInAssignment => false;
            public override ClrTypeCache ClrTypeCache { get; }
            public override BoundUnaryOperatorFactory BoundUnaryOperatorFactory { get; }
            public override BoundBinaryOperatorFactory BoundBinaryOperatorFactory { get; }
        }

        internal sealed class TypeBinder : Binder
        {
        }

        internal sealed class FunctionBinder : Binder
        {
            public FunctionBinder(FunctionSymbol functionSymbol)
            {
                FunctionSymbol = functionSymbol;
            }

            public override FunctionSymbol FunctionSymbol { get; }
        }
    }
}
