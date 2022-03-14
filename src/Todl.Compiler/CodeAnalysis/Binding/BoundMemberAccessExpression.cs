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
        public MemberInfo MemberInfo { get; internal init; }
        public string MemberName => MemberInfo.Name;
        public bool IsStatic => BoundBaseExpression is BoundTypeExpression;

        public override TypeSymbol ResultType
        {
            get
            {
                var clrType = (MemberInfo.MemberType == MemberTypes.Property)
                    ? (MemberInfo as PropertyInfo).PropertyType
                    : (MemberInfo as FieldInfo).FieldType;

                return ClrTypeSymbol.MapClrType(clrType);
            }
        }

        public override bool Constant
            => MemberInfo is FieldInfo fieldInfo && fieldInfo.IsLiteral;
    }

    public partial class Binder
    {
        private BoundMemberAccessExpression BindMemberAccessExpression(
            MemberAccessExpression memberAccessExpression)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var boundBaseExpression = BindExpression(memberAccessExpression.BaseExpression);

            Debug.Assert(boundBaseExpression.ResultType.IsNative);

            var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
            var memberInfo = type
                .GetMember(memberAccessExpression.MemberIdentifierToken.Text.ToString())
                .FirstOrDefault();

            if (memberInfo is null)
            {
                diagnosticBuilder.Add(
                    new Diagnostic()
                    {
                        Message = $"Member {memberAccessExpression.MemberIdentifierToken.Text} does not exist in type {type.FullName}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = memberAccessExpression.MemberIdentifierToken.GetTextLocation(),
                        ErrorCode = ErrorCode.MemberNotFound
                    });
            }

            return BoundNodeFactory.CreateBoundMemberAccessExpression(
                syntaxNode: memberAccessExpression,
                boundBaseExpression: boundBaseExpression,
                memberInfo: memberInfo,
                diagnosticBuilder: diagnosticBuilder);
        }
    }
}
