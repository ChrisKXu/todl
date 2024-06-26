﻿namespace Todl.Compiler.Tests;

public class TestClass
{
    public string PublicStringField;
    public int PublicIntField;
    public bool PublicBoolField;

    public static string PublicStaticStringField;
    public static int PublicStaticIntField;
    public static bool PublicStaticBoolField;

    public string PublicStringProperty { get; set; }
    public int PublicIntProperty { get; set; }
    public bool PublicBoolProperty { get; set; }

    public static string PublicStaticStringProperty { get; set; }
    public static int PublicStaticIntProperty { get; set; }
    public static bool PublicStaticBoolProperty { get; set; }


    public static readonly TestClass Instance = new();
}
