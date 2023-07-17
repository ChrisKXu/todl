using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todl.Compiler.Tests;

public class TestClass
{
    public string PublicStringField;
    public int PublicIntField;

    public static string PublicStaticStringField;
    public static int PublicStaticIntField;

    public string PublicStringProperty { get; set; }
    public int PublicIntProperty { get; set; }

    public static string PublicStaticStringProperty { get; set; }
    public static int PublicStaticIntProperty { get; set; }

    public static readonly TestClass Instance = new();
}
