using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed record SyntaxTrivia(SyntaxKind Kind, TextSpan Text) { }
