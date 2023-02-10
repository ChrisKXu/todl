using System;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding;

public abstract class ConstantValue
{
    public virtual TypeSymbol ResultType { get; internal init; }

    public virtual string StringValue => throw new InvalidOperationException();

    public virtual bool BooleanValue => throw new InvalidOperationException();

    public virtual float FloatValue => throw new InvalidOperationException();

    public virtual double DoubleValue => throw new InvalidOperationException();

    public virtual int Int32Value => throw new InvalidOperationException();

    public virtual uint UInt32Value => throw new InvalidOperationException();

    public virtual long Int64Value => Int32Value;

    public virtual ulong UInt64Value => UInt32Value;
}

public sealed class ConstantValueFactory
{
    private readonly BuiltInTypes builtInTypes;
    private readonly ConstantBooleanValue trueValue;
    private readonly ConstantBooleanValue falseValue;

    public ConstantNullValue Null { get; }

    public ConstantValueFactory(BuiltInTypes builtInTypes)
    {
        this.builtInTypes = builtInTypes;

        trueValue = new() { ResultType = builtInTypes.Boolean, Value = true };
        falseValue = new() { ResultType = builtInTypes.Boolean, Value = false };

        Null = new() { ResultType = builtInTypes.Object };
    }

    public ConstantStringValue Create(string value)
        => new()
        {
            Value = value,
            ResultType = builtInTypes.String
        };

    public ConstantBooleanValue Create(bool value)
        => value ? trueValue : falseValue;

    public ConstantFloatValue Create(float value)
        => new()
        {
            Value = value,
            ResultType = builtInTypes.Float
        };

    public ConstantDoubleValue Create(double value)
        => new()
        {
            Value = value,
            ResultType = builtInTypes.Double
        };

    public ConstantInt32Value Create(int value)
        => new()
        {
            Value = value,
            ResultType = builtInTypes.Int32
        };

    public ConstantUInt32Value Create(uint value)
        => new()
        {
            Value = value,
            ResultType = builtInTypes.UInt32
        };

    public ConstantInt64Value Create(long value)
        => new()
        {
            Value = value,
            ResultType = builtInTypes.Int64
        };

    public ConstantUInt64Value Create(ulong value)
        => new()
        {
            Value = value,
            ResultType = builtInTypes.UInt64
        };
}

#pragma warning disable 0659

public sealed class ConstantNullValue : ConstantValue
{
    public static readonly ConstantNullValue Null = new();

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || obj is null;
}

public sealed class ConstantStringValue : ConstantValue
{
    public string Value { get; internal init; }

    public override string StringValue => Value;

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || StringValue.Equals(obj);
}

public sealed class ConstantBooleanValue : ConstantValue
{
    public bool Value { get; internal init; }

    public override bool BooleanValue => Value;

    public override int Int32Value => BooleanValue ? 1 : 0;

    public override uint UInt32Value => BooleanValue ? 1u : 0u;

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || BooleanValue.Equals(obj);
}

public sealed class ConstantFloatValue : ConstantValue
{
    // In parity to C#, we fold fp values in double precision
    // so we need to store them in double
    public double Value { get; internal init; }

    public override float FloatValue => (float)Value;

    public override double DoubleValue => Value;

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || FloatValue.Equals(obj);
}

public sealed class ConstantDoubleValue : ConstantValue
{
    public double Value { get; internal init; }

    public override double DoubleValue => Value;

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || DoubleValue.Equals(obj);
}

public sealed class ConstantInt32Value : ConstantValue
{
    public int Value { get; internal init; }

    public override int Int32Value => Value;

    public override uint UInt32Value => unchecked((uint)Value);

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || Int32Value.Equals(obj);
}

public sealed class ConstantUInt32Value : ConstantValue
{
    public uint Value { get; internal init; }

    public override int Int32Value => unchecked((int)Value);

    public override uint UInt32Value => Value;

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || UInt32Value.Equals(obj);
}

public sealed class ConstantInt64Value : ConstantValue
{
    public long Value { get; internal init; }

    public override long Int64Value => Value;

    public override ulong UInt64Value => unchecked((ulong)Value);

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || Int64Value.Equals(obj);
}

public sealed class ConstantUInt64Value : ConstantValue
{
    public ulong Value { get; internal init; }

    public override long Int64Value => unchecked((long)Value);

    public override ulong UInt64Value => Value;

    public override bool Equals(object obj)
        => ReferenceEquals(this, obj) || UInt64Value.Equals(obj);
}

#pragma warning restore 0659
