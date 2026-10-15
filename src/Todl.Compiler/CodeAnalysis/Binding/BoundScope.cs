using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundScope
    {
        private readonly HashSet<Symbol> symbols = new();

        public BoundScope Parent { get; }
        public BoundScopeKind BoundScopeKind { get; }

        private BoundScope(BoundScope parent, BoundScopeKind boundScopeKind)
        {
            Parent = parent;
            BoundScopeKind = boundScopeKind;
        }

        public VariableSymbol LookupVariable(string name)
            => LookupSymbol<VariableSymbol>(name);

        private VariableSymbol LookupLocalVariable(string name)
            => LookupLocalSymbol<VariableSymbol>(name);

        /// <summary>
        /// Finds a variable named <paramref name="name"/> that a new declaration in this scope would collide with:
        /// one declared in this scope or in an enclosing block, up to and including the function scope.
        /// </summary>
        public VariableSymbol FindConflictingVariable(string name)
        {
            for (var scope = this; scope is not null; scope = scope.Parent)
            {
                var variable = scope.LookupLocalVariable(name);
                if (variable is not null)
                {
                    return variable;
                }

                if (scope.BoundScopeKind != BoundScopeKind.BlockStatement)
                {
                    break;
                }
            }

            return null;
        }

        public VariableSymbol DeclareVariable(VariableSymbol variable)
        {
            var existingVariable = LookupLocalVariable(variable.Name);
            if (existingVariable != null)
            {
                return existingVariable;
            }

            symbols.Add(variable);

            return variable;
        }

        public FunctionSymbol DeclareFunction(FunctionSymbol function)
        {
            symbols.Add(function);

            var existingFunction = symbols
                .OfType<FunctionSymbol>()
                .FirstOrDefault(f => f.IsAmbigousWith(function));

            if (existingFunction is not null)
            {
                return existingFunction;
            }

            return function;
        }

        public FunctionSymbol LookupFunctionSymbol(FunctionDeclarationMember functionDeclarationMember)
        {
            var symbol = symbols
                .OfType<FunctionSymbol>()
                .FirstOrDefault(f => f.FunctionDeclarationMember == functionDeclarationMember);

            return symbol ?? Parent?.LookupFunctionSymbol(functionDeclarationMember);
        }

        public FunctionSymbol LookupFunctionSymbol(string name, IReadOnlyDictionary<string, TypeSymbol> namedArguments)
        {
            var symbol = symbols
                .OfType<FunctionSymbol>()
                .FirstOrDefault(f => f.Match(name, namedArguments));

            return symbol ?? Parent?.LookupFunctionSymbol(name, namedArguments);
        }

        public FunctionSymbol LookupFunctionSymbol(string name, IEnumerable<TypeSymbol> positionalArguments)
        {
            var symbol = symbols
                .OfType<FunctionSymbol>()
                .FirstOrDefault(f => f.Match(name, positionalArguments));

            return symbol ?? Parent?.LookupFunctionSymbol(name, positionalArguments);
        }

        public TSymbol LookupSymbol<TSymbol>(string name) where TSymbol : Symbol
        {
            var symbol = LookupLocalSymbol<TSymbol>(name);
            return symbol ?? Parent?.LookupSymbol<TSymbol>(name);
        }

        private TSymbol LookupLocalSymbol<TSymbol>(string name) where TSymbol : Symbol
        {
            return symbols.Where(s => s.Name == name).OfType<TSymbol>().FirstOrDefault();
        }

        public BoundScope CreateChildScope(BoundScopeKind boundScopeKind) => new(this, boundScopeKind);

        public static readonly BoundScope GlobalScope = new(null, BoundScopeKind.Global);
    }

    public enum BoundScopeKind
    {
        Global,
        Module,
        Type,
        Function,
        BlockStatement
    }
}
