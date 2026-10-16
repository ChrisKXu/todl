﻿using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

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

    public virtual BoundExceptionContext ExceptionContext
        => Parent?.ExceptionContext ?? BoundExceptionContext.Empty;

    public virtual DiagnosticBag.Builder DiagnosticBuilder
        => Parent?.DiagnosticBuilder;

    public bool IsInFunction => FunctionSymbol is not null;

    internal ClrTypeCacheView GetClrTypeCacheView(SyntaxTree syntaxTree)
        => ClrTypeCache.CreateView(syntaxTree.Directives.OfType<ImportDirective>());

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

    public static Binder CreateScriptBinder(ClrTypeCache clrTypeCache, DiagnosticBag.Builder diagnosticBuilder)
        => new ScriptBinder(clrTypeCache, diagnosticBuilder)
        {
            Scope = BoundScope.GlobalScope
        };

    public static Binder CreateModuleBinder(ClrTypeCache clrTypeCache, ConstantValueFactory constantValueFactory, DiagnosticBag.Builder diagnosticBuilder)
        => new ModuleBinder(clrTypeCache, constantValueFactory, diagnosticBuilder)
        {
            Scope = BoundScope.GlobalScope.CreateChildScope(BoundScopeKind.Module)
        };

    public Binder CreateTypeBinder()
        => new TypeBinder()
        {
            Parent = this,
            Scope = Scope.CreateChildScope(BoundScopeKind.Type)
        };

    public Binder CreateLoopBinder(LoopLabel loopLabel)
    {
        var context = BoundLoopContext?.CreateChildContext(loopLabel) ?? new BoundLoopContext() { LoopLabel = loopLabel };

        if (loopLabel is not null)
        {
            var parent = context.Parent;
            while (parent is not null)
            {
                if (parent.LoopLabel is not null
                    && parent.LoopLabel.Label.GetText().Equals(loopLabel.Label.GetText()))
                {
                    ReportDiagnostic(new Diagnostic()
                    {
                        ErrorCode = ErrorCode.DuplicateLoopLabel,
                        Level = DiagnosticLevel.Error,
                        TextLocation = loopLabel.GetTextLocation(),
                        Message = $"Duplicate loop label '{loopLabel.Label.GetText()}'"
                    });
                }

                parent = parent.Parent;
            }
        }

        return new LoopBinder(context)
        {
            Parent = this,
            Scope = Scope.CreateChildScope(BoundScopeKind.BlockStatement)
        };
    }

    private Binder CreateExceptionContextBinder(BoundExceptionContext exceptionContext)
        => new ExceptionContextBinder(exceptionContext)
        {
            Parent = this,
            Scope = Scope.CreateChildScope(BoundScopeKind.BlockStatement)
        };

    /// <summary>
    /// Resolves the <see cref="BoundLoopContext"/> that a <c>break</c>/<c>continue</c>
    /// statement targets: the innermost enclosing loop when unlabeled, or the ancestor loop whose
    /// declared label matches when labeled. Reports <see cref="ErrorCode.NoEnclosingLoop"/> when there
    /// is no enclosing loop at all, or <see cref="ErrorCode.UndefinedLoopLabel"/> when a label doesn't
    /// match any enclosing loop, and returns null in both cases.
    /// </summary>
    private BoundLoopContext ResolveLoopContext(NameExpression label, TextLocation statementTextLocation)
    {
        if (BoundLoopContext is null)
        {
            ReportDiagnostic(new Diagnostic()
            {
                Level = DiagnosticLevel.Error,
                ErrorCode = ErrorCode.NoEnclosingLoop,
                Message = "No enclosing loop out of which to break or continue.",
                TextLocation = statementTextLocation
            });

            return null;
        }

        if (label is null)
        {
            return ValidateJumpTarget(BoundLoopContext, statementTextLocation);
        }

        var labelText = label.GetText();
        for (var context = BoundLoopContext; context is not null; context = context.Parent)
        {
            if (context.LoopLabel is not null && context.LoopLabel.Label.GetText().Equals(labelText))
            {
                return ValidateJumpTarget(context, statementTextLocation);
            }
        }

        ReportDiagnostic(new Diagnostic()
        {
            Level = DiagnosticLevel.Error,
            ErrorCode = ErrorCode.UndefinedLoopLabel,
            Message = $"No enclosing loop is labeled '{labelText}'.",
            TextLocation = label.GetTextLocation()
        });

        return null;
    }

    /// <summary>
    /// Declares a local variable in the current scope, reporting <see cref="ErrorCode.DuplicateVariableDeclaration"/>
    /// when its name is already declared in the same block or an enclosing block or parameter list of the function.
    /// </summary>
    private void DeclareLocalVariable(VariableSymbol variable, TextLocation declarationLocation)
    {
        if (Scope.FindConflictingVariable(variable.Name) is not null)
        {
            ReportDiagnostic(new Diagnostic()
            {
                ErrorCode = ErrorCode.DuplicateVariableDeclaration,
                Level = DiagnosticLevel.Error,
                TextLocation = declarationLocation,
                Message = $"A variable named '{variable.Name}' is already declared in this scope or an enclosing scope."
            });
        }

        Scope.DeclareVariable(variable);
    }

    private BoundLoopContext ValidateJumpTarget(BoundLoopContext target, TextLocation statementTextLocation)
    {
        if (ExceptionContext.ExitsFinally(target))
        {
            ReportDiagnostic(new Diagnostic()
            {
                Level = DiagnosticLevel.Error,
                ErrorCode = ErrorCode.JumpOutOfFinally,
                Message = "A break or continue statement cannot exit a finally block.",
                TextLocation = statementTextLocation
            });
        }

        return target;
    }

    protected void ReportDiagnostic(Diagnostic diagnostic)
        => DiagnosticBuilder.Add(diagnostic);

    internal sealed class ScriptBinder : Binder
    {
        public ScriptBinder(ClrTypeCache clrTypeCache, DiagnosticBag.Builder diagnosticBuilder)
        {
            ClrTypeCache = clrTypeCache;
            BoundBinaryOperatorFactory = new();
            ConstantValueFactory = new(clrTypeCache.BuiltInTypes);
            DiagnosticBuilder = diagnosticBuilder;
        }

        public override bool AllowVariableDeclarationInAssignment => true;
        public override ClrTypeCache ClrTypeCache { get; }
        public override BoundBinaryOperatorFactory BoundBinaryOperatorFactory { get; }
        public override ConstantValueFactory ConstantValueFactory { get; }
        public override DiagnosticBag.Builder DiagnosticBuilder { get; }
    }

    internal sealed class ModuleBinder : Binder
    {
        public ModuleBinder(ClrTypeCache clrTypeCache, ConstantValueFactory constantValueFactory, DiagnosticBag.Builder diagnosticBuilder)
        {
            ClrTypeCache = clrTypeCache;
            BoundBinaryOperatorFactory = new();
            ConstantValueFactory = constantValueFactory;
            DiagnosticBuilder = diagnosticBuilder;
        }

        public override bool AllowVariableDeclarationInAssignment => false;
        public override ClrTypeCache ClrTypeCache { get; }
        public override BoundBinaryOperatorFactory BoundBinaryOperatorFactory { get; }
        public override ConstantValueFactory ConstantValueFactory { get; }
        public override DiagnosticBag.Builder DiagnosticBuilder { get; }
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

        // a function never inherits except/finally state from where it is declared
        public override BoundExceptionContext ExceptionContext { get; } = BoundExceptionContext.Empty;
    }

    internal sealed class ExceptionContextBinder : Binder
    {
        internal ExceptionContextBinder(BoundExceptionContext exceptionContext)
        {
            ExceptionContext = exceptionContext;
        }

        public override BoundExceptionContext ExceptionContext { get; }
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
