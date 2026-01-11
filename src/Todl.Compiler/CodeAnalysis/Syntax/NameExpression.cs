using System.Collections.Immutable;
using System.Text;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

/// <summary>
/// Abstract base class for all name expressions.
/// </summary>
public abstract class NameExpression : Expression
{
    /// <summary>
    /// Returns the canonical name for CLR type lookup (with dots).
    /// e.g., "System::Console" becomes "System.Console"
    /// </summary>
    public abstract string CanonicalName { get; }

    /// <summary>
    /// Returns the rightmost simple name.
    /// </summary>
    public abstract SimpleNameExpression GetUnqualifiedName();
}

/// <summary>
/// Represents a simple identifier name (e.g., "Console", "x", "int").
/// Can resolve to either a type or a variable in the binder.
/// </summary>
public sealed class SimpleNameExpression : NameExpression
{
    public SyntaxToken IdentifierToken { get; internal init; }

    public override TextSpan Text => IdentifierToken.Text;
    public override string CanonicalName => IdentifierToken.Text.ToString();
    public override SimpleNameExpression GetUnqualifiedName() => this;
}

/// <summary>
/// Represents a namespace-qualified name using the :: operator.
/// Always resolves to a type in the binder (never a variable).
/// Example: System::Console, System::Collections::Generic::List
///
/// Uses a flat structure: namespace parts are stored in an array,
/// not as nested expressions.
/// </summary>
public sealed class NamespaceQualifiedNameExpression : NameExpression
{
    /// <summary>
    /// The namespace part identifiers (e.g., [System, Collections, Generic] for System::Collections::Generic::List).
    /// </summary>
    public ImmutableArray<SyntaxToken> NamespaceIdentifiers { get; internal init; }

    /// <summary>
    /// The type name (the rightmost identifier, e.g., "List").
    /// </summary>
    public SyntaxToken TypeIdentifierToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(NamespaceIdentifiers[0].Text, TypeIdentifierToken.Text);

    public override string CanonicalName
    {
        get
        {
            var builder = new StringBuilder();
            foreach (var ns in NamespaceIdentifiers)
            {
                builder.Append(ns.Text);
                builder.Append('.');
            }
            builder.Append(TypeIdentifierToken.Text);
            return builder.ToString();
        }
    }

    public override SimpleNameExpression GetUnqualifiedName()
        => new SimpleNameExpression
        {
            SyntaxTree = SyntaxTree,
            IdentifierToken = TypeIdentifierToken
        };
}

public sealed partial class Parser
{
    /// <summary>
    /// Parses a name expression, which can be:
    /// - SimpleNameExpression: identifier or built-in type keyword
    /// - NamespaceQualifiedNameExpression: namespace::namespace::...::typeName
    /// </summary>
    private NameExpression ParseNameExpression()
    {
        // Handle built-in type keywords (int, string, bool, etc.)
        if (SyntaxFacts.BuiltInTypes.Contains(Current.Kind))
        {
            return new SimpleNameExpression()
            {
                SyntaxTree = syntaxTree,
                IdentifierToken = ExpectToken(Current.Kind)
            };
        }

        var firstIdentifier = ExpectToken(SyntaxKind.IdentifierToken);

        // Check if this is a namespace-qualified name
        if (Current.Kind != SyntaxKind.ColonColonToken)
        {
            // Simple name - just an identifier
            return new SimpleNameExpression()
            {
                SyntaxTree = syntaxTree,
                IdentifierToken = firstIdentifier
            };
        }

        // Namespace-qualified name: collect all parts
        var namespaceIdentifiers = ImmutableArray.CreateBuilder<SyntaxToken>();
        namespaceIdentifiers.Add(firstIdentifier);

        while (Current.Kind == SyntaxKind.ColonColonToken && Peak.Kind == SyntaxKind.IdentifierToken)
        {
            ExpectToken(SyntaxKind.ColonColonToken); // consume but don't store
            var nextIdentifier = ExpectToken(SyntaxKind.IdentifierToken);

            // Check if there's another :: coming - if so, this is a namespace part
            // If not, this is the type name
            if (Current.Kind == SyntaxKind.ColonColonToken)
            {
                namespaceIdentifiers.Add(nextIdentifier);
            }
            else
            {
                // This is the final type name
                return new NamespaceQualifiedNameExpression()
                {
                    SyntaxTree = syntaxTree,
                    NamespaceIdentifiers = namespaceIdentifiers.ToImmutable(),
                    TypeIdentifierToken = nextIdentifier
                };
            }
        }

        // This shouldn't be reached in normal parsing
        throw new System.InvalidOperationException("Unexpected state in ParseNameExpression");
    }
}
