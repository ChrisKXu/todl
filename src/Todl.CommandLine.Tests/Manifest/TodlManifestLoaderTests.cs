using System;
using System.IO;
using FluentAssertions;
using Todl.CommandLine.Manifest;
using Xunit;

namespace Todl.CommandLine.Tests.Manifest;

public sealed class TodlManifestLoaderTests
{
    [Fact]
    public void TestParseMinimalManifestDefaultsVersionAndNugetPackages()
    {
        var manifest = TodlManifestLoader.Parse("""{ "name": "hello" }""");

        manifest.Name.Should().Be("hello");
        manifest.Version.Should().Be("0.1.0");
        manifest.NugetPackages.Should().BeEmpty();
    }

    [Fact]
    public void TestParseExplicitVersionOverridesDefault()
    {
        var manifest = TodlManifestLoader.Parse("""{ "name": "hello", "version": "2.3.4" }""");

        manifest.Version.Should().Be("2.3.4");
    }

    [Fact]
    public void TestParseNugetPackagePlainVersionString()
    {
        var manifest = TodlManifestLoader.Parse("""
            {
                "name": "hello",
                "nugetPackages": { "Newtonsoft.Json": "13.0.3" }
            }
            """);

        manifest.NugetPackages.Should().ContainKey("Newtonsoft.Json")
            .WhoseValue.Should().Be(new NugetPackageReference { Version = "13.0.3" });
    }

    [Fact]
    public void TestParseNugetPackageExpandedObjectShape()
    {
        var manifest = TodlManifestLoader.Parse("""
            {
                "name": "hello",
                "nugetPackages": {
                    "Contoso.Internal": { "version": "1.4.0", "source": "https://nuget.contoso.internal/v3/index.json" }
                }
            }
            """);

        manifest.NugetPackages.Should().ContainKey("Contoso.Internal")
            .WhoseValue.Should().Be(new NugetPackageReference
            {
                Version = "1.4.0",
                Source = "https://nuget.contoso.internal/v3/index.json"
            });
    }

    [Theory]
    [InlineData("""{ "version": "1.0.0" }""")]
    [InlineData("""{ "name": "", "version": "1.0.0" }""")]
    [InlineData("""{ "name": "   " }""")]
    public void TestParseMissingOrEmptyNameThrowsTodlManifestException(string json)
    {
        var act = () => TodlManifestLoader.Parse(json);

        act.Should().Throw<TodlManifestException>()
            .WithMessage("*name*");
    }

    [Theory]
    [InlineData("{ not valid json")]
    [InlineData("""{ "name": "hello", }""")]
    public void TestParseMalformedJsonThrowsTodlManifestException(string json)
    {
        var act = () => TodlManifestLoader.Parse(json);

        act.Should().Throw<TodlManifestException>()
            .WithMessage("*malformed JSON*");
    }

    [Fact]
    public void TestParseNugetPackageEntryMissingVersionThrowsTodlManifestException()
    {
        var json = """
            {
                "name": "hello",
                "nugetPackages": { "Contoso.Internal": { "source": "https://example.com" } }
            }
            """;

        var act = () => TodlManifestLoader.Parse(json);

        act.Should().Throw<TodlManifestException>();
    }

    [Fact]
    public void TestResolveManifestPathForDirectoryAppendsManifestFileName()
    {
        using var tempDirectory = new TempDirectory();

        var resolved = TodlManifestLoader.ResolveManifestPath(tempDirectory.Path);

        resolved.Should().Be(Path.Combine(tempDirectory.Path, "todl.json"));
    }

    [Fact]
    public void TestResolveManifestPathForFileReturnsItDirectly()
    {
        using var tempDirectory = new TempDirectory();
        var manifestPath = Path.Combine(tempDirectory.Path, "todl.json");
        File.WriteAllText(manifestPath, """{ "name": "hello" }""");

        var resolved = TodlManifestLoader.ResolveManifestPath(manifestPath);

        resolved.Should().Be(manifestPath);
    }

    [Fact]
    public void TestResolveManifestPathForNullOrEmptyPathDefaultsToCurrentDirectory()
    {
        var resolved = TodlManifestLoader.ResolveManifestPath(null);

        resolved.Should().Be(Path.Combine(Environment.CurrentDirectory, "todl.json"));
    }

    [Fact]
    public void TestLoadDirectoryWithManifestLoadsSuccessfully()
    {
        using var tempDirectory = new TempDirectory();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "todl.json"), """{ "name": "sample-project" }""");

        var manifest = TodlManifestLoader.Load(tempDirectory.Path);

        manifest.Name.Should().Be("sample-project");
    }

    [Fact]
    public void TestLoadMissingManifestThrowsTodlManifestException()
    {
        using var tempDirectory = new TempDirectory();

        var act = () => TodlManifestLoader.Load(tempDirectory.Path);

        act.Should().Throw<TodlManifestException>()
            .WithMessage("*Could not find*");
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("fibonacci")]
    [InlineData("Fibonacci.Loop")]
    public void TestLoadSampleManifestsParseSuccessfully(string sampleName)
    {
        var samplePath = Path.Combine(TestPaths.SamplesDirectory, sampleName);

        var manifest = TodlManifestLoader.Load(samplePath);

        manifest.Name.Should().NotBeNullOrWhiteSpace();
        manifest.Version.Should().Be("0.1.0");
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("todl-manifest-tests-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
