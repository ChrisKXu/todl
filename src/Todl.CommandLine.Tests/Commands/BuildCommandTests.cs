using System;
using System.CommandLine;
using System.IO;
using FluentAssertions;
using Todl.CommandLine.Commands;
using Xunit;

namespace Todl.CommandLine.Tests.Commands;

public sealed class BuildCommandTests
{
    [Fact]
    public void TestInvokeBuildWithValidProjectReturnsZeroAndWritesOutputArtifacts()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "todl.json"), """{ "name": "sample-project", "version": "9.9.9" }""");
        File.WriteAllText(Path.Combine(tempDirectory.Path, "main.tdl"), """
            import { Console } from System;

            void Main() {
                Console.WriteLine("hello from a test");
            }
            """);

        var (exitCode, stdOut, stdErr) = Invoke("build", tempDirectory.Path);

        exitCode.Should().Be(0, because: stdErr);
        var outputAssemblyPath = Path.Combine(tempDirectory.Path, "out", "sample-project.dll");
        var runtimeConfigPath = Path.Combine(tempDirectory.Path, "out", "sample-project.runtimeconfig.json");
        File.Exists(outputAssemblyPath).Should().BeTrue();
        File.Exists(runtimeConfigPath).Should().BeTrue();
        stdOut.Should().Contain(outputAssemblyPath);
    }

    [Fact]
    public void TestInvokeBuildWithNoSourceFilesReturnsNonZero()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "todl.json"), """{ "name": "sample-project", "version": "9.9.9" }""");

        var (exitCode, _, stdErr) = Invoke("build", tempDirectory.Path);

        exitCode.Should().NotBe(0);
        stdErr.Should().Contain(".tdl");
    }

    [Fact]
    public void TestInvokeBuildWithCompileErrorReturnsNonZeroAndPrintsDiagnosticToStdErr()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "todl.json"), """{ "name": "sample-project", "version": "9.9.9" }""");
        var sourcePath = Path.Combine(tempDirectory.Path, "main.tdl");
        File.WriteAllText(sourcePath, """
            import { Console } from System;

            void Main() {
                Console.WriteLine(getValue());
            }

            int getValue() {
                return "not a number";
            }
            """);

        var (exitCode, _, stdErr) = Invoke("build", tempDirectory.Path);

        exitCode.Should().NotBe(0);
        stdErr.Should().Contain(sourcePath).And.Contain("TypeMismatch");
        File.Exists(Path.Combine(tempDirectory.Path, "out", "sample-project.dll")).Should().BeFalse();
    }

    [Fact]
    public void TestInvokeBuildWithMissingManifestReturnsNonZeroAndPrintsDiagnosticToStdErr()
    {
        using var tempDirectory = new TempDirectory();

        var (exitCode, _, stdErr) = Invoke("build", tempDirectory.Path);

        exitCode.Should().NotBe(0);
        stdErr.Should().Contain("todl.json");
    }

    private static (int ExitCode, string StdOut, string StdErr) Invoke(params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var stdOut = new StringWriter();
        using var stdErr = new StringWriter();

        try
        {
            Console.SetOut(stdOut);
            Console.SetError(stdErr);

            var rootCommand = new RootCommand("test");
            rootCommand.Add(new BuildCommand());
            var exitCode = rootCommand.Parse(args).Invoke();

            return (exitCode, stdOut.ToString(), stdErr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("todl-build-command-tests-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
