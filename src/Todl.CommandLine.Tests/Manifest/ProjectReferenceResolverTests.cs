using System;
using System.IO;
using FluentAssertions;
using Todl.CommandLine.Manifest;
using Xunit;

namespace Todl.CommandLine.Tests.Manifest;

public sealed class ProjectReferenceResolverTests
{
    [Fact]
    public void ResolveCsprojPathWithExactlyOneCsprojReturnsIt()
    {
        using var tempDirectory = new TempDirectory();
        var libDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "MyLib")).FullName;
        var csprojPath = Path.Combine(libDirectory, "MyLib.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var resolved = ProjectReferenceResolver.ResolveCsprojPath(tempDirectory.Path, "MyLib", "MyLib");

        resolved.Should().Be(csprojPath);
    }

    [Fact]
    public void ResolveCsprojPathNormalizesForwardSlashesOnAnyOs()
    {
        using var tempDirectory = new TempDirectory();
        var libDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "nested", "MyLib")).FullName;
        var csprojPath = Path.Combine(libDirectory, "MyLib.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var resolved = ProjectReferenceResolver.ResolveCsprojPath(tempDirectory.Path, "nested/MyLib", "MyLib");

        resolved.Should().Be(csprojPath);
    }

    [Fact]
    public void ResolveCsprojPathWithMissingFolderThrowsTodlManifestException()
    {
        using var tempDirectory = new TempDirectory();

        var act = () => ProjectReferenceResolver.ResolveCsprojPath(tempDirectory.Path, "DoesNotExist", "MyLib");

        act.Should().Throw<TodlManifestException>()
            .WithMessage("*DoesNotExist*does not exist*");
    }

    [Fact]
    public void ResolveCsprojPathWithNoCsprojThrowsTodlManifestException()
    {
        using var tempDirectory = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "MyLib"));

        var act = () => ProjectReferenceResolver.ResolveCsprojPath(tempDirectory.Path, "MyLib", "MyLib");

        act.Should().Throw<TodlManifestException>()
            .WithMessage("*no .csproj file*");
    }

    [Fact]
    public void ResolveCsprojPathWithMultipleCsprojThrowsTodlManifestExceptionNamingEachOne()
    {
        using var tempDirectory = new TempDirectory();
        var libDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "MyLib")).FullName;
        File.WriteAllText(Path.Combine(libDirectory, "A.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(Path.Combine(libDirectory, "B.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var act = () => ProjectReferenceResolver.ResolveCsprojPath(tempDirectory.Path, "MyLib", "MyLib");

        act.Should().Throw<TodlManifestException>()
            .WithMessage("*A.csproj*")
            .WithMessage("*B.csproj*");
    }

    [Fact]
    public void ResolveCsprojPathDoesNotRecurseIntoSubfolders()
    {
        using var tempDirectory = new TempDirectory();
        var libDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "MyLib")).FullName;
        File.WriteAllText(Path.Combine(libDirectory, "MyLib.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var nestedDirectory = Directory.CreateDirectory(Path.Combine(libDirectory, "tests")).FullName;
        File.WriteAllText(Path.Combine(nestedDirectory, "MyLib.Tests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var resolved = ProjectReferenceResolver.ResolveCsprojPath(tempDirectory.Path, "MyLib", "MyLib");

        resolved.Should().Be(Path.Combine(libDirectory, "MyLib.csproj"));
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("todl-project-reference-resolver-tests-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
