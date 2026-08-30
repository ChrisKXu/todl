using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Todl.CommandLine.Commands;
using Xunit;

namespace Todl.CommandLine.Tests.Commands;

// End-to-end: todl build resolving a local project reference (todl-design's
// ideas/tooling/project-file-format-plan.md Step 5 "done when" criterion) —
// builds the referenced .csproj in-process via MSBuild, resolves its output
// into the compilation's references, and produces a runnable artifact.
// Exercises a real MSBuild restore+build, so it is slower than the other
// BuildCommand tests.
public sealed class ProjectReferenceBuildCommandTests
{
    [Fact]
    public void BuildWithLocalProjectReferenceCallsIntoReferencedProjectAtRuntime()
    {
        using var tempDirectory = new TempDirectory();

        var libDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "MyLib")).FullName;
        File.WriteAllText(Path.Combine(libDirectory, "MyLib.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(libDirectory, "Greeter.cs"), """
            namespace MyLib;

            public static class Greeter
            {
                public static string Greet() => "hello from MyLib";
            }
            """);

        var consumerDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "consumer")).FullName;
        File.WriteAllText(Path.Combine(consumerDirectory, "todl.json"), """
            { "name": "consumer-project", "nugetPackages": { "MyLib": "../MyLib" } }
            """);
        File.WriteAllText(Path.Combine(consumerDirectory, "main.tdl"), """
            import { Console } from System;
            import { Greeter } from MyLib;

            void Main() {
                Console.WriteLine(Greeter.Greet());
            }
            """);

        var (exitCode, stdOut, stdErr) = Invoke("build", consumerDirectory);

        exitCode.Should().Be(0, because: stdErr);

        var outputAssemblyPath = Path.Combine(consumerDirectory, "out", "consumer-project.dll");
        var referencedAssemblyCopyPath = Path.Combine(consumerDirectory, "out", "MyLib.dll");
        File.Exists(outputAssemblyPath).Should().BeTrue();
        File.Exists(referencedAssemblyCopyPath).Should().BeTrue(because: "MyLib.dll must be copy-local'd beside the output for the runtime host to find it");
        stdOut.Should().Contain(outputAssemblyPath);

        var runResult = RunDotnet(outputAssemblyPath);
        runResult.ExitCode.Should().Be(0, because: runResult.StdErr);
        runResult.StdOut.Should().Contain("hello from MyLib");
    }

    [Fact]
    public void BuildWithProjectReferenceFolderContainingNoCsprojReturnsNonZeroAndPrintsDiagnostic()
    {
        using var tempDirectory = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "MyLib"));
        File.WriteAllText(Path.Combine(tempDirectory.Path, "todl.json"), """
            { "name": "consumer-project", "nugetPackages": { "MyLib": "MyLib" } }
            """);
        File.WriteAllText(Path.Combine(tempDirectory.Path, "main.tdl"), """
            void Main() {
            }
            """);

        var (exitCode, _, stdErr) = Invoke("build", tempDirectory.Path);

        exitCode.Should().NotBe(0);
        stdErr.Should().Contain("no .csproj file");
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

    private static (int ExitCode, string StdOut, string StdErr) RunDotnet(string assemblyPath)
    {
        using var process = Process.Start(new ProcessStartInfo("dotnet", $"\"{assemblyPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdOut, stdErr);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("todl-project-reference-build-command-tests-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
