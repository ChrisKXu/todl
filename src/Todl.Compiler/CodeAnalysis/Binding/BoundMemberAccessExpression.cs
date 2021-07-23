using System;
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
        public bool IsStatic { get; internal init; }
    }

    public enum BoundMemberAccessKind
    {
        Property,
        Field,
        Function
    }

    public sealed partial class Binder
    {
        private BoundExpression BindMemberAccessExpression(
            BoundScope scope,
            MemberAccessExpression memberAccessExpression)
        {
            var boundBaseExpression = BindExpression(scope, memberAccessExpression.BaseExpression);

            if (boundBaseExpression is BoundNamespaceExpression boundNamespaceExpression)
            {
                if (loadedNamespaces.Contains(memberAccessExpression.QualifiedName))
                {
                    return new BoundNamespaceExpression() { Namespace = memberAccessExpression.QualifiedName };
                }

                if (loadedTypes.ContainsKey(memberAccessExpression.QualifiedName))
                {
                    return new BoundTypeExpression()
                    {
                        ResultType = ClrTypeSymbol.MapClrType(loadedTypes[memberAccessExpression.QualifiedName])
                    };
                }

                return ReportErrorExpression(
                    new Diagnostic(
                        message: $"Invalid member {memberAccessExpression.MemberIdentifierToken.Text}",
                        level: DiagnosticLevel.Error,
                        memberAccessExpression.MemberIdentifierToken.GetTextLocation()));
            }

            if (boundBaseExpression is BoundMemberAccessExpression boundMemberAccessExpression)
            {
                if (boundMemberAccessExpression.BoundMemberAccessKind == BoundMemberAccessKind.Function)
                {
                    return ReportErrorExpression(
                        new Diagnostic(
                            message: $"Invalid member {memberAccessExpression.MemberIdentifierToken.Text}",
                            level: DiagnosticLevel.Error,
                            memberAccessExpression.MemberIdentifierToken.GetTextLocation()));
                }
            }

            if (!boundBaseExpression.ResultType.IsNative)
            {
                return ReportErrorExpression(
                    new Diagnostic(
                        message: $"Type {boundBaseExpression.ResultType} is not supported.",
                        level: DiagnosticLevel.Error,
                        memberAccessExpression.MemberIdentifierToken.GetTextLocation()));
            }

            var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
            var isStatic = boundBaseExpression is BoundTypeExpression;
            var memberInfo = type.GetMember(memberAccessExpression.MemberIdentifierToken.Text.ToString());

            if (!memberInfo.Any())
            {
                return ReportErrorExpression(
                    new Diagnostic(
                        message: $"Member {memberAccessExpression.MemberIdentifierToken.Text} does not exist",
                        level: DiagnosticLevel.Error,
                        memberAccessExpression.MemberIdentifierToken.GetTextLocation()));
            }

            // if there are multiple members, make sure these are all overloads of the same method
            if (memberInfo.Length > 1 && memberInfo.Any(m => m.MemberType != MemberTypes.Method))
            {
                return ReportErrorExpression(
                    new Diagnostic(
                        message: $"Member {memberAccessExpression.MemberIdentifierToken.Text} is not unique",
                        level: DiagnosticLevel.Error,
                        memberAccessExpression.MemberIdentifierToken.GetTextLocation()));
            }

            return memberInfo[0].MemberType switch
            {
                MemberTypes.Property => new BoundMemberAccessExpression()
                {
                    BoundBaseExpression = boundBaseExpression,
                    MemberName = memberAccessExpression.MemberIdentifierToken,
                    ResultType = ClrTypeSymbol.MapClrType((memberInfo[0] as PropertyInfo).PropertyType),
                    BoundMemberAccessKind = BoundMemberAccessKind.Property,
                    IsStatic = isStatic
                },

                MemberTypes.Field => new BoundMemberAccessExpression()
                {
                    BoundBaseExpression = boundBaseExpression,
                    MemberName = memberAccessExpression.MemberIdentifierToken,
                    ResultType = ClrTypeSymbol.MapClrType((memberInfo[0] as FieldInfo).FieldType),
                    BoundMemberAccessKind = BoundMemberAccessKind.Field,
                    IsStatic = isStatic
                },

                MemberTypes.Method => new BoundMemberAccessExpression()
                {
                    BoundBaseExpression = boundBaseExpression,
                    MemberName = memberAccessExpression.MemberIdentifierToken,
                    ResultType = TypeSymbol.ClrVoid, // note that this is not the return type of the function
                    BoundMemberAccessKind = BoundMemberAccessKind.Function,
                    IsStatic = isStatic
                },

                _ => null
            };
        }
    }
}
