﻿using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

/// <summary>
/// Binder reorganizes SyntaxTree elements into Bound counterparts
/// and prepares necessary information for Emitter to use in the emit process
/// </summary>
public partial class Binder
{
    public BoundScope Scope { get; private init; }
    public Binder Parent { get; private init; }

    public virtual BoundBinaryOperatorFactory BoundBinaryOperatorFactory
        => Parent?.BoundBinaryOperatorFactory;

    public virtual ClrTypeCache ClrTypeCache
        => Parent?.ClrTypeCache;

    public virtual ConstantValueFactory ConstantValueFactory
        => Parent?.ConstantValueFactory;

    public virtual bool AllowVariableDeclarationInAssignment
        => Parent.AllowVariableDeclarationInAssignment;

    public virtual FunctionSymbol FunctionSymbol
        => Parent?.FunctionSymbol;

    public virtual BoundLoopContext BoundLoopContext
        => Parent?.BoundLoopContext;

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

    public Binder CreateLoopBinder()
        => new LoopBinder(BoundLoopContext?.CreateChildContext() ?? new BoundLoopContext())
        {
            Parent = this,
            Scope = Scope.CreateChildScope(BoundScopeKind.BlockStatement)
        };

    internal sealed class ScriptBinder : Binder
    {
        public ScriptBinder(ClrTypeCache clrTypeCache)
        {
            ClrTypeCache = clrTypeCache;
            BoundBinaryOperatorFactory = new(clrTypeCache);
            ConstantValueFactory = new(clrTypeCache.BuiltInTypes);
        }

        public override bool AllowVariableDeclarationInAssignment => true;
        public override ClrTypeCache ClrTypeCache { get; }
        public override BoundBinaryOperatorFactory BoundBinaryOperatorFactory { get; }
        public override ConstantValueFactory ConstantValueFactory { get; }
    }

    internal sealed class ModuleBinder : Binder
    {
        public ModuleBinder(ClrTypeCache clrTypeCache)
        {
            ClrTypeCache = clrTypeCache;
            BoundBinaryOperatorFactory = new(clrTypeCache);
            ConstantValueFactory = new(clrTypeCache.BuiltInTypes);
        }

        public override bool AllowVariableDeclarationInAssignment => false;
        public override ClrTypeCache ClrTypeCache { get; }
        public override BoundBinaryOperatorFactory BoundBinaryOperatorFactory { get; }
        public override ConstantValueFactory ConstantValueFactory { get; }
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

    internal sealed partial class LoopBinder : Binder
    {
        internal LoopBinder(BoundLoopContext boundLoopContext)
        {
            BoundLoopContext = boundLoopContext;
        }

        public override BoundLoopContext BoundLoopContext { get; }
    }
}
