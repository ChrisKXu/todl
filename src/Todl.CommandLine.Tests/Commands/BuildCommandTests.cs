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
    public void Invoke_ValidManifest_ReturnsZeroAndPrintsManifestInfo()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "todl.json"), """{ "name": "sample-project", "version": "9.9.9" }""");

        var (exitCode, stdOut, _) = Invoke("build", tempDirectory.Path);

        exitCode.Should().Be(0);
        stdOut.Should().Contain("sample-project").And.Contain("9.9.9");
    }

    [Fact]
    public void Invoke_MissingManifest_ReturnsNonZeroAndPrintsDiagnosticToStdErr()
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
