namespace Todl.Compiler.Tests;

public class TestClass
{
    public string PublicStringField;
    public int PublicIntField;
    public bool PublicBoolField;
    public object PublicObjectField;

    public static string PublicStaticStringField;
    public static int PublicStaticIntField;
    public static bool PublicStaticBoolField;

    public string PublicStringProperty { get; set; }
    public int PublicIntProperty { get; set; }
    public bool PublicBoolProperty { get; set; }

    public static string PublicStaticStringProperty { get; set; }
    public static int PublicStaticIntProperty { get; set; }
    public static bool PublicStaticBoolProperty { get; set; }

    public int this[int index]
    {
        get => 0;
        set { }
    }

    public static readonly TestClass Instance = new();
}

public struct TestStruct
{
    public TestStruct(int value) { Value = value; }

    public int Value { get; set; }

    public int this[int index]
    {
        get => Value + index;
        set => Value = value;
    }
}

public static class NullableTestHelper
{
    public static int? Echo(int? value) => value;

    public static int? Make(bool hasValue, int value) => hasValue ? value : null;

    public static string Describe(int? value) => value.HasValue ? "value:" + value.Value : "empty";

    public static string Pick(int value) => "plain";
    public static string Pick(int? value) => "nullable";

    public static string Only(long? value) => "long?";

    public static string Both(int? value) => "int?";
    public static string Both(long? value) => "long?";

    public static string Named(int? first, bool? second) => $"{first}-{second}";
}

public sealed class NullableTestHolder
{
    public int? Field;
    public double? Property { get; set; }

    public NullableTestHolder() { }

    public NullableTestHolder(int? field) { Field = field; }
}
