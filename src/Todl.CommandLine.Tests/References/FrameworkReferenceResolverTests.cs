using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Todl.CommandLine.References;
using Xunit;

namespace Todl.CommandLine.Tests.References;

public sealed class FrameworkReferenceResolverTests
{
    [Fact]
    public void GetTargetFrameworkShouldReturnNet10()
    {
        var (tfm, band) = FrameworkReferenceResolver.GetTargetFramework();

        tfm.Should().Be("net10.0");
        band.Should().Be(new Version(10, 0));
    }

    [Fact]
    public void ResolveShouldReturnFrameworkAssemblies()
    {
        var refs = FrameworkReferenceResolver.Resolve();

        refs.Compile.Should().NotBeEmpty();
        refs.Compile.Select(a => a.Name).Should().Contain(["System.Runtime", "System.Console", "System.Linq"]);
    }

    [Fact]
    public void ResolveShouldNotReturnAnalyzerAssemblies()
    {
        var refs = FrameworkReferenceResolver.Resolve();

        refs.Compile.Should().NotContain(a => a.Path.Contains("analyzers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolvedAssemblyPathsShouldAllExist()
    {
        var refs = FrameworkReferenceResolver.Resolve();

        refs.Compile.Should().OnlyContain(a => File.Exists(a.Path));
    }

    [Fact]
    public void ResolvedAssemblyNamesShouldBeUnique()
    {
        var refs = FrameworkReferenceResolver.Resolve();

        refs.Compile.Select(a => a.Name)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ResolvedFrameworkVersionShouldBeBandFloor()
    {
        var refs = FrameworkReferenceResolver.Resolve();

        refs.Framework.Version.Should().Be("10.0.0");
        refs.Framework.Tfm.Should().Be("net10.0");
        refs.Framework.Name.Should().Be("Microsoft.NETCore.App");
    }

    [Fact]
    public void ResolveShouldHonorTodlRefPackOverride()
    {
        using var tempDirectory = new TempDirectory();
        var dataDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "data"));

        var fakeAssemblyOnePath = Path.Combine(tempDirectory.Path, "FakeOne.dll");
        var fakeAssemblyTwoPath = Path.Combine(tempDirectory.Path, "FakeTwo.dll");
        File.WriteAllText(fakeAssemblyOnePath, string.Empty);
        File.WriteAllText(fakeAssemblyTwoPath, string.Empty);

        File.WriteAllText(Path.Combine(dataDirectory.FullName, "FrameworkList.xml"),
            $"""
            <FileList TargetFrameworkIdentifier=".NETCoreApp" TargetFrameworkVersion="10.0"
                      FrameworkName="Microsoft.NETCore.App" Name=".NET Runtime">
              <File Type="Managed" Path="FakeOne.dll" AssemblyName="FakeOne" PublicKeyToken="b03f5f7f11d50a3a"
                    AssemblyVersion="10.0.0.0" FileVersion="10.0.0.0" />
              <File Type="Managed" Path="FakeTwo.dll" AssemblyName="FakeTwo" PublicKeyToken="b03f5f7f11d50a3a"
                    AssemblyVersion="10.0.0.0" FileVersion="10.0.0.0" />
              <File Type="Analyzer" Path="analyzers/FakeAnalyzer.dll" AssemblyName="FakeAnalyzer"
                    AssemblyVersion="10.0.0.0" FileVersion="10.0.0.0" />
            </FileList>
            """);

        var originalOverride = Environment.GetEnvironmentVariable("TODL_REF_PACK");
        try
        {
            Environment.SetEnvironmentVariable("TODL_REF_PACK", tempDirectory.Path);

            var refs = FrameworkReferenceResolver.Resolve();

            refs.Compile.Should().HaveCount(2);
            refs.Compile.Select(a => a.Name).Should().BeEquivalentTo(["FakeOne", "FakeTwo"]);
            refs.Compile.Should().OnlyContain(a => File.Exists(a.Path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TODL_REF_PACK", originalOverride);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("todl-ref-pack-tests-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
