using System;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Symbols;

public sealed class LocalVariableSymbol : VariableSymbol
{
    public VariableDeclarationStatement VariableDeclarationStatement { get; internal init; }
    public BoundExpression BoundInitializer { get; internal init; }

    public override string Name
        => VariableDeclarationStatement.IdentifierToken.Text.ToString();

    public override bool ReadOnly
        => VariableDeclarationStatement.DeclarationKeyword.Kind == SyntaxKind.ConstKeywordToken;

    public override TypeSymbol Type => BoundInitializer.ResultType;
    public override bool Constant => BoundInitializer.Constant;

    public override bool Equals(Symbol other)
        => other is LocalVariableSymbol localVariableSymbol
        && VariableDeclarationStatement.Equals(localVariableSymbol.VariableDeclarationStatement);

    public override int GetHashCode()
        => HashCode.Combine(VariableDeclarationStatement);
}
