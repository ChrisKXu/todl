using System.Collections.Generic;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundConversionExpressionTests
{
    [Fact]
    public void ResultTypeAndConstantnessComeFromTargetTypeAndOperand()
    {
        var operand = TestUtils.BindExpression<BoundConstant>("5");
        var targetType = TestDefaults.DefaultClrTypeCache.BuiltInTypes.Int64;

        var boundConversionExpression = BoundNodeFactory.CreateBoundConversionExpression(
            syntaxNode: operand.SyntaxNode,
            operand: operand,
            targetType: targetType,
            conversionKind: ConversionKind.ImplicitNumeric);

        boundConversionExpression.ResultType.Should().Be(targetType);
        boundConversionExpression.Constant.Should().BeTrue();
        boundConversionExpression.LValue.Should().BeFalse();
        boundConversionExpression.ConversionKind.Should().Be(ConversionKind.ImplicitNumeric);
    }

    [Fact]
    public void ConstantIsFalseWhenOperandIsNotConstant()
    {
        // property access is never a compile-time constant, unlike a const field access
        var operand = TestUtils.BindExpression<BoundClrPropertyAccessExpression>("\"abc\".Length");

        var boundConversionExpression = BoundNodeFactory.CreateBoundConversionExpression(
            syntaxNode: operand.SyntaxNode,
            operand: operand,
            targetType: TestDefaults.DefaultClrTypeCache.BuiltInTypes.Int64,
            conversionKind: ConversionKind.ImplicitNumeric);

        boundConversionExpression.Constant.Should().BeFalse();
    }

    [Fact]
    public void WalkerVisitsOperandAndReturnsTheSameNode()
    {
        var operand = TestUtils.BindExpression<BoundConstant>("5");
        var boundConversionExpression = BoundNodeFactory.CreateBoundConversionExpression(
            syntaxNode: operand.SyntaxNode,
            operand: operand,
            targetType: TestDefaults.DefaultClrTypeCache.BuiltInTypes.Int64,
            conversionKind: ConversionKind.ImplicitNumeric);

        var walker = new RecordingBoundTreeWalker();
        var result = boundConversionExpression.Accept(walker);

        result.Should().BeSameAs(boundConversionExpression);
        walker.Visited.Should().Contain(operand);
    }

    [Fact]
    public void RewriterReconstructsTheNodeWhenOperandChanges()
    {
        var operand = TestUtils.BindExpression<BoundConstant>("5");
        var replacement = TestUtils.BindExpression<BoundConstant>("6");
        var targetType = TestDefaults.DefaultClrTypeCache.BuiltInTypes.Int64;
        var boundConversionExpression = BoundNodeFactory.CreateBoundConversionExpression(
            syntaxNode: operand.SyntaxNode,
            operand: operand,
            targetType: targetType,
            conversionKind: ConversionKind.ImplicitNumeric);

        var rewriter = new ReplacingBoundTreeRewriter(operand, replacement);
        var result = rewriter.Visit(boundConversionExpression).As<BoundConversionExpression>();

        result.Should().NotBeSameAs(boundConversionExpression);
        result.Operand.Should().BeSameAs(replacement);
        result.TargetType.Should().Be(targetType);
        result.ConversionKind.Should().Be(ConversionKind.ImplicitNumeric);
    }

    [Fact]
    public void RewriterReturnsTheSameNodeWhenNothingChanges()
    {
        var operand = TestUtils.BindExpression<BoundConstant>("5");
        var boundConversionExpression = BoundNodeFactory.CreateBoundConversionExpression(
            syntaxNode: operand.SyntaxNode,
            operand: operand,
            targetType: TestDefaults.DefaultClrTypeCache.BuiltInTypes.Int64,
            conversionKind: ConversionKind.ImplicitNumeric);

        var rewriter = new ReplacingBoundTreeRewriter(from: null, to: null);
        var result = rewriter.Visit(boundConversionExpression);

        result.Should().BeSameAs(boundConversionExpression);
    }

    private sealed class RecordingBoundTreeWalker : BoundTreeWalker
    {
        public List<BoundNode> Visited { get; } = new();

        public override BoundNode Visit(BoundNode node)
        {
            if (node is not null)
            {
                Visited.Add(node);
            }

            return base.Visit(node);
        }
    }

    private sealed class ReplacingBoundTreeRewriter : BoundTreeRewriter
    {
        private readonly BoundNode from;
        private readonly BoundNode to;

        public ReplacingBoundTreeRewriter(BoundNode from, BoundNode to)
        {
            this.from = from;
            this.to = to;
        }

        public override BoundNode Visit(BoundNode node)
            => node is not null && node == from ? to : base.Visit(node);
    }
}
