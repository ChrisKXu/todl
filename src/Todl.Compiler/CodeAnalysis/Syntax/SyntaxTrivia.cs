using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public readonly record struct SyntaxTrivia(SyntaxKind Kind, TextSpan Text) { }
