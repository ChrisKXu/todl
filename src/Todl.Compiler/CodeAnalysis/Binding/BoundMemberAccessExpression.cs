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
        private BoundExpression BindMemberAccessExpression(
            BoundScope scope,
            MemberAccessExpression memberAccessExpression)
        {
            var boundBaseExpression = BindExpression(scope, memberAccessExpression.BaseExpression);

            Debug.Assert(boundBaseExpression.ResultType.IsNative);

            var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
            var memberInfo = type.GetMember(memberAccessExpression.MemberIdentifierToken.Text.ToString());

            if (!memberInfo.Any())
            {
                return ReportErrorExpression(
                    new Diagnostic()
                    {
                        Message = $"Member {memberAccessExpression.MemberIdentifierToken.Text} does not exist in type {type.FullName}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = memberAccessExpression.MemberIdentifierToken.GetTextLocation(),
                        ErrorCode = ErrorCode.MemberNotFound
                    });
            }

            // if there are multiple members, make sure these are all overloads of the same method
            Debug.Assert(memberInfo.Length == 1 || memberInfo.All(m => m.MemberType == MemberTypes.Method));

            return memberInfo[0].MemberType switch
            {
                MemberTypes.Property => new BoundMemberAccessExpression()
                {
                    SyntaxNode = memberAccessExpression,
                    BoundBaseExpression = boundBaseExpression,
                    MemberName = memberAccessExpression.MemberIdentifierToken,
                    ResultType = ClrTypeSymbol.MapClrType((memberInfo[0] as PropertyInfo).PropertyType),
                    BoundMemberAccessKind = BoundMemberAccessKind.Property
                },

                MemberTypes.Field => new BoundMemberAccessExpression()
                {
                    SyntaxNode = memberAccessExpression,
                    BoundBaseExpression = boundBaseExpression,
                    MemberName = memberAccessExpression.MemberIdentifierToken,
                    ResultType = ClrTypeSymbol.MapClrType((memberInfo[0] as FieldInfo).FieldType),
                    BoundMemberAccessKind = BoundMemberAccessKind.Field
                },

                _ => new BoundErrorExpression()
            };
        }
    }
}
