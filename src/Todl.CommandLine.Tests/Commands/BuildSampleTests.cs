using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Todl.CommandLine.Commands;
using Xunit;

namespace Todl.CommandLine.Tests.Commands;

/// <summary>
/// End-to-end smoke test: builds each sample with the real todl build
/// pipeline, launches the emitted assembly with `dotnet`, and asserts its
/// stdout matches the sample's expectation. This is the only test that
/// proves the emitted AssemblyRefs are correct and the app actually launches
/// — a test that only checks the build exit code proves nothing about the
/// reference set.
/// </summary>
public sealed class BuildSampleTests
{
    [Theory]
    [InlineData("hello")]
    [InlineData("fibonacci")]
    [InlineData("Fibonacci.Loop")]
    public void TestBuildAndRunSampleProducesExpectedStdOut(string sampleName)
    {
        var sampleDirectory = Path.Combine(TestPaths.SamplesDirectory, sampleName);
        using var outputDirectory = new TempDirectory();

        var rootCommand = new RootCommand("test");
        rootCommand.Add(new BuildCommand());
        var buildExitCode = rootCommand.Parse(["build", sampleDirectory, "--output", outputDirectory.Path]).Invoke();

        buildExitCode.Should().Be(0);

        var assemblyPath = Path.Combine(outputDirectory.Path, $"{sampleName}.dll");
        var runtimeConfigPath = Path.Combine(outputDirectory.Path, $"{sampleName}.runtimeconfig.json");
        File.Exists(assemblyPath).Should().BeTrue($"'{assemblyPath}' should have been emitted");
        File.Exists(runtimeConfigPath).Should().BeTrue($"'{runtimeConfigPath}' should have been emitted");

        var expectedStdOut = File.ReadAllText(Path.Combine(sampleDirectory, "stdout.expected.txt"));

        var startInfo = new ProcessStartInfo("dotnet", $"\"{assemblyPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo)!;
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        process.ExitCode.Should().Be(0, because: stdErr);
        Normalize(stdOut).Should().Be(Normalize(expectedStdOut));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\n');

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("todl-build-sample-tests-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
