using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundMemberAccessExpression : BoundExpression
    {
        public BoundExpression BoundBaseExpression { get; internal init; }
        public BoundMemberAccessKind BoundMemberAccessKind { get; internal init; }
        public SyntaxToken MemberName { get; internal init; }
        public override TypeSymbol ResultType { get; internal init; }
        public bool IsStatic => BoundBaseExpression is BoundTypeExpression;
    }

    public enum BoundMemberAccessKind
    {
        Property,
        Field
    }

    public sealed partial class Binder
    {
        private BoundMemberAccessExpression BindMemberAccessExpression(
            BoundScope scope,
            MemberAccessExpression memberAccessExpression)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var boundBaseExpression = BindExpression(scope, memberAccessExpression.BaseExpression);
            diagnosticBuilder.Add(boundBaseExpression);

            Debug.Assert(boundBaseExpression.ResultType.IsNative);

            var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
            var memberInfo = type.GetMember(memberAccessExpression.MemberIdentifierToken.Text.ToString());

            if (!memberInfo.Any())
            {
                diagnosticBuilder.Add(
                    new Diagnostic()
                    {
                        Message = $"Member {memberAccessExpression.MemberIdentifierToken.Text} does not exist in type {type.FullName}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = memberAccessExpression.MemberIdentifierToken.GetTextLocation(),
                        ErrorCode = ErrorCode.MemberNotFound
                    });

                return new()
                {
                    SyntaxNode = memberAccessExpression,
                    BoundBaseExpression = boundBaseExpression,
                    DiagnosticBuilder = diagnosticBuilder
                };
            }

            // if there are multiple members, make sure these are all overloads of the same method
            Debug.Assert(memberInfo.Length == 1 || memberInfo.All(m => m.MemberType == MemberTypes.Method));

            return memberInfo[0].MemberType switch
            {
                MemberTypes.Property => new()
                {
                    SyntaxNode = memberAccessExpression,
                    BoundBaseExpression = boundBaseExpression,
                    MemberName = memberAccessExpression.MemberIdentifierToken,
                    ResultType = ClrTypeSymbol.MapClrType((memberInfo[0] as PropertyInfo).PropertyType),
                    BoundMemberAccessKind = BoundMemberAccessKind.Property,
                    DiagnosticBuilder = diagnosticBuilder
                },

                MemberTypes.Field => new()
                {
                    SyntaxNode = memberAccessExpression,
                    BoundBaseExpression = boundBaseExpression,
                    MemberName = memberAccessExpression.MemberIdentifierToken,
                    ResultType = ClrTypeSymbol.MapClrType((memberInfo[0] as FieldInfo).FieldType),
                    BoundMemberAccessKind = BoundMemberAccessKind.Field,
                    DiagnosticBuilder = diagnosticBuilder
                },

                _ => null // should not happen
            };
        }
    }
}
