using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class TryStatementTests
{
    private const string Prelude = """
        import {
            Exception, Int32, FormatException, DivideByZeroException, InvalidOperationException,
            ArgumentException, ArgumentNullException
        } from System;

        void Log(string[] log, string text) { log[0] = log[0] + text; }

        """;

    public static IEnumerable<object[]> Cases()
    {
        // source declaring void Run(string[] log), expected content of log[0]
        yield return new object[]
        {
            """
            void Run(string[] log) {
                let divisor = Int32.Parse("0");
                try { let x = 10 / divisor; Log(log, "unreached"); } except DivideByZeroException { Log(log, "div"); }
            }
            """,
            "div"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { let n = Int32.Parse("abc"); Log(log, "unreached"); } except FormatException { Log(log, "format"); }
            }
            """,
            "format"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { Log(log, "body;"); } except Exception { Log(log, "unreached"); }
                Log(log, "after");
            }
            """,
            "body;after"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new ArgumentNullException("p"); }
                except InvalidOperationException { Log(log, "ioe"); }
                except ArgumentNullException { Log(log, "null"); }
                except Exception { Log(log, "exception"); }
            }
            """,
            "null"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new ArgumentException("p"); }
                except ArgumentNullException { Log(log, "null"); }
                except ArgumentException { Log(log, "arg"); }
                except Exception { Log(log, "exception"); }
            }
            """,
            "arg"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new InvalidOperationException("p"); }
                except ArgumentException { Log(log, "arg"); }
                except Exception { Log(log, "exception"); }
            }
            """,
            "exception"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new InvalidOperationException("p"); }
                except FormatException { Log(log, "format"); }
                except { Log(log, "general"); }
            }
            """,
            "general"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { let n = Int32.Parse("abc"); } except { Log(log, "general"); }
            }
            """,
            "general"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new Exception("boom"); } except Exception e { Log(log, e.Message); }
            }
            """,
            "boom"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new Exception("one"); } except Exception first { Log(log, first.Message); }
                try { throw new Exception("two"); } except Exception second { Log(log, second.Message); }
            }
            """,
            "onetwo"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new ArgumentNullException("p"); } except ArgumentException e { Log(log, "base:" + e.ParamName); }
            }
            """,
            "base:p"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    try { throw new InvalidOperationException("orig"); }
                    except InvalidOperationException e { Log(log, "inner:" + e.Message + ";"); throw; }
                } except Exception e2 { Log(log, "outer:" + e2.Message); }
            }
            """,
            "inner:orig;outer:orig"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { Log(log, "t;"); } finally { Log(log, "f;"); }
                Log(log, "after");
            }
            """,
            "t;f;after"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new Exception("e"); } except Exception { Log(log, "c;"); } finally { Log(log, "f;"); }
                Log(log, "after");
            }
            """,
            "c;f;after"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { Log(log, "t;"); } except Exception { Log(log, "c;"); } finally { Log(log, "f;"); }
                Log(log, "after");
            }
            """,
            "t;f;after"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    try { throw new Exception("e"); } finally { Log(log, "f;"); }
                } except Exception { Log(log, "outer"); }
            }
            """,
            "f;outer"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    try { throw new Exception("e"); } except FormatException { Log(log, "wrong;"); } finally { Log(log, "f;"); }
                } except Exception e { Log(log, "outer:" + e.Message); }
            }
            """,
            "f;outer:e"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    try { throw new Exception("original"); }
                    finally { throw new InvalidOperationException("replacement"); }
                } except Exception e { Log(log, e.Message); }
            }
            """,
            "replacement"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    try { throw new Exception("original"); }
                    except Exception { throw new InvalidOperationException("from handler"); }
                    finally { Log(log, "f;"); }
                } except InvalidOperationException e { Log(log, e.Message); }
            }
            """,
            "f;from handler"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    try { Log(log, "a;"); }
                    finally { Log(log, "b;"); }
                } finally { Log(log, "c;"); }
            }
            """,
            "a;b;c;"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    try { throw new FormatException("f"); } except FormatException { Log(log, "1;"); throw new InvalidOperationException("i"); }
                } except InvalidOperationException { Log(log, "2;"); }
                try { Log(log, "3;"); try { Log(log, "4;"); } except { } } except { }
            }
            """,
            "1;2;3;4;"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { throw new Exception("e"); }
                except Exception {
                    try { throw new InvalidOperationException("inner"); } except InvalidOperationException e { Log(log, e.Message); }
                }
            }
            """,
            "inner"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { Log(log, "t;"); }
                finally {
                    try { throw new Exception("e"); } except Exception { Log(log, "c;"); }
                    Log(log, "f;");
                }
            }
            """,
            "t;c;f;"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try { } except Exception { }
                try { } finally { }
                Log(log, "empty");
            }
            """,
            "empty"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                let i = 0;
                while i < 3 {
                    i = i + 1;
                    try {
                        if i == 2 { throw new Exception("e"); }
                        Log(log, "t" + i.ToString() + ";");
                    } except Exception { Log(log, "c" + i.ToString() + ";"); }
                }
            }
            """,
            "t1;c2;t3;"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                let i = 0;
                while i < 4 {
                    i = i + 1;
                    try {
                        if i == 1 { continue; }
                        if i == 3 { break; }
                        Log(log, "t" + i.ToString() + ";");
                    } finally { Log(log, "f" + i.ToString() + ";"); }
                }
                Log(log, "end");
            }
            """,
            "f1;t2;f2;f3;end"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                let i = 0;
                while i < 4 {
                    i = i + 1;
                    try {
                        if i == 2 { continue; }
                        if i == 3 { break; }
                        Log(log, "t" + i.ToString() + ";");
                    } except Exception { Log(log, "unreached"); } finally { Log(log, "f" + i.ToString() + ";"); }
                }
            }
            """,
            "t1;f1;f2;f3;"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                let i = 0;
                while i < 3 {
                    i = i + 1;
                    try {
                        throw new Exception("e");
                    } except Exception {
                        if i == 1 { continue; }
                        break;
                    } finally { Log(log, "f" + i.ToString() + ";"); }
                }
                Log(log, "end");
            }
            """,
            "f1;f2;end"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    let i = 0;
                    while i < 3 {
                        i = i + 1;
                        if i == 2 { throw new Exception("e"); }
                        Log(log, "l" + i.ToString() + ";");
                    }
                } except Exception { Log(log, "c;"); }
            }
            """,
            "l1;c;"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                try {
                    let i = 0;
                    while true {
                        i = i + 1;
                        if i == 3 { break; }
                        Log(log, "l" + i.ToString() + ";");
                    }
                    Log(log, "out;");
                } finally { Log(log, "f;"); }
            }
            """,
            "l1;l2;out;f;"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                let i = 0;
                while true : outer {
                    i = i + 1;
                    while true : inner {
                        try {
                            if i == 1 { continue outer; }
                            break outer;
                        } finally { Log(log, "f" + i.ToString() + ";"); }
                    }
                }
                Log(log, "end");
            }
            """,
            "f1;f2;end"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                while true : outer {
                    try {
                        try {
                            while true { break outer; }
                        } finally { Log(log, "inner;"); }
                    } finally { Log(log, "outer;"); }
                }
                Log(log, "end");
            }
            """,
            "inner;outer;end"
        };
        yield return new object[]
        {
            """
            void Run(string[] log) {
                let i = 0;
                while i < 2 {
                    i = i + 1;
                    try {
                        while true {
                            try { break; } finally { Log(log, "a;"); }
                        }
                        Log(log, "b;");
                    } finally { Log(log, "c;"); }
                }
            }
            """,
            "a;b;c;a;b;c;"
        };
        yield return new object[]
        {
            """
            int F(string[] log) {
                try { return 1; } finally { Log(log, "f;"); }
                return 0;
            }
            void Run(string[] log) { Log(log, F(log).ToString()); }
            """,
            "f;1"
        };
        yield return new object[]
        {
            """
            int F(string[] log) {
                let x = 1;
                try { return x; } finally { x = 2; Log(log, "f" + x.ToString() + ";"); }
                return 0;
            }
            void Run(string[] log) { Log(log, F(log).ToString()); }
            """,
            "f2;1"
        };
        yield return new object[]
        {
            """
            int F(string[] log) {
                try { throw new Exception("e"); }
                except Exception { return 2; }
                finally { Log(log, "f;"); }
                return 0;
            }
            void Run(string[] log) { Log(log, F(log).ToString()); }
            """,
            "f;2"
        };
        yield return new object[]
        {
            """
            int F(string[] log) {
                try {
                    try { return 1; } finally { Log(log, "a;"); }
                } finally { Log(log, "b;"); }
                return 0;
            }
            void Run(string[] log) { Log(log, F(log).ToString()); }
            """,
            "a;b;1"
        };
        yield return new object[]
        {
            """
            int F(string[] log) {
                let i = 0;
                while true {
                    i = i + 1;
                    try { if i == 3 { return i; } } finally { Log(log, "f" + i.ToString() + ";"); }
                }
                return 0;
            }
            void Run(string[] log) { Log(log, F(log).ToString()); }
            """,
            "f1;f2;f3;3"
        };
        yield return new object[]
        {
            """
            int ParseOr(string text, int fallback) {
                return Int32.Parse(text);
            } except FormatException {
                return fallback;
            }
            void Run(string[] log) {
                Log(log, ParseOr("12", 0).ToString() + ";");
                Log(log, ParseOr("x", 7).ToString());
            }
            """,
            "12;7"
        };
        yield return new object[]
        {
            """
            void Work(string[] log, int mode) {
                Log(log, "body;");
                if mode == 1 { throw new InvalidOperationException("io"); }
                Log(log, "end;");
            } except InvalidOperationException e {
                Log(log, "caught:" + e.Message + ";");
            } finally {
                Log(log, "finally;");
            }
            void Run(string[] log) { Work(log, 0); Work(log, 1); }
            """,
            "body;end;finally;body;caught:io;finally;"
        };
        yield return new object[]
        {
            """
            int Work(string[] log, string text) {
                return Int32.Parse(text);
            } except FormatException {
                Log(log, "format;");
                return -1;
            } finally {
                Log(log, "finally;");
            }
            void Run(string[] log) { Log(log, Work(log, "5").ToString() + ";"); Log(log, Work(log, "x").ToString()); }
            """,
            "finally;5;format;finally;-1"
        };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TryProgramShouldProduceExpectedLog(string source, string expected)
    {
        var log = new[] { "" };
        RunProgram($"{Prelude}{source}\nvoid Main() {{}}", assembly =>
        {
            assembly.EntryPoint.DeclaringType.GetMethod("Run").Invoke(null, new object[] { log });
        });

        log[0].Should().Be(expected);
    }

    [Fact]
    public void FunctionLevelHandlersOnMainShouldRun()
    {
        var log = new[] { "" };
        RunProgram(
            Prelude + """
            void Main(string[] log) {
                throw new Exception("m");
            } except Exception e {
                Log(log, e.Message + ";");
            } finally {
                Log(log, "f");
            }
            """,
            assembly => assembly.EntryPoint.Invoke(null, new object[] { log }));

        log[0].Should().Be("m;f");
    }

    [Fact]
    public void FunctionLevelFinallyOnMainShouldRunOnNormalExit()
    {
        var log = new[] { "" };
        RunProgram(
            Prelude + """
            void Main(string[] log) {
                Log(log, "body;");
            } finally {
                Log(log, "f");
            }
            """,
            assembly => assembly.EntryPoint.Invoke(null, new object[] { log }));

        log[0].Should().Be("body;f");
    }

    [Fact]
    public void UnhandledExceptionShouldPropagate()
    {
        RunProgram(
            Prelude + """
            void Run(string[] log) {
                try { throw new InvalidOperationException("unhandled"); } except FormatException { Log(log, "wrong"); }
            }
            void Main() {}
            """,
            assembly =>
            {
                var action = () => assembly.EntryPoint.DeclaringType.GetMethod("Run").Invoke(null, new object[] { new[] { "" } });
                action.Should().Throw<TargetInvocationException>()
                    .WithInnerException<InvalidOperationException>().WithMessage("unhandled");
            });
    }

    [Fact]
    public void RethrowShouldPropagateTheOriginalExceptionInstance()
    {
        RunProgram(
            Prelude + """
            void Fail() { throw new InvalidOperationException("orig"); }
            void Run(string[] log) {
                try { Fail(); } except InvalidOperationException { throw; }
            }
            void Main() {}
            """,
            assembly =>
            {
                var action = () => assembly.EntryPoint.DeclaringType.GetMethod("Run").Invoke(null, new object[] { new[] { "" } });
                var exception = action.Should().Throw<TargetInvocationException>()
                    .WithInnerException<InvalidOperationException>().WithMessage("orig").Which;

                exception.StackTrace.Should().Contain("Fail");
            });
    }

    private static void RunProgram(string source, Action<Assembly> action)
    {
        var (assemblyDefinition, _) = TestUtils.Compile(SourceText.FromString(source));
        assemblyDefinition.Should().NotBeNull();
        TestUtils.RunAssembly(assemblyDefinition, action);
    }
}
