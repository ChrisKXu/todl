using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using Todl.CommandLine.Manifest;
using Todl.CommandLine.References;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;

namespace Todl.CommandLine.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Build a Todl project")
    {
        var outputPathOption = new Option<string>("--output");
        outputPathOption.Aliases.Add("-o");

        var pathArgument = new Argument<string>("path") { Arity = ArgumentArity.ZeroOrOne };

        Add(outputPathOption);
        Add(pathArgument);

        SetAction((parseResult) =>
        {
            var path = parseResult.GetValue(pathArgument);
            var outputOption = parseResult.GetValue(outputPathOption);

            TodlManifest manifest;
            try
            {
                manifest = TodlManifestLoader.Load(path);
            }
            catch (TodlManifestException ex)
            {
                Console.Error.WriteLine($"error: {ex.Message}");
                return 1;
            }

            var manifestPath = TodlManifestLoader.ResolveManifestPath(path);
            var projectDirectory = Path.GetDirectoryName(manifestPath)!;
            var outputDirectory = Path.GetFullPath(string.IsNullOrEmpty(outputOption)
                ? Path.Combine(projectDirectory, "out")
                : outputOption);

            Version assemblyVersion;
            try
            {
                assemblyVersion = Version.Parse(manifest.Version);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                Console.Error.WriteLine($"error: '{manifestPath}' has an invalid 'version' value '{manifest.Version}': {ex.Message}");
                return 1;
            }

            // Cheap checks first: fail on a source-free project before paying
            // for framework reference resolution and MetadataLoadContext setup.
            var sources = Directory.EnumerateFiles(projectDirectory, "*.tdl", SearchOption.AllDirectories)
                .Where(file => !IsUnderDirectory(file, outputDirectory))
                .Select(SourceText.FromFile)
                .ToArray();

            if (sources.Length == 0)
            {
                Console.Error.WriteLine($"error: no .tdl source files found under '{projectDirectory}'.");
                return 1;
            }

            ResolvedReferences references;
            try
            {
                references = FrameworkReferenceResolver.Resolve();
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            var assemblyResolver = new PathAssemblyResolver(references.Compile.Select(r => r.Path).ToArray());

            // Compilation's constructor takes ownership of metadataLoadContext and
            // disposes it (see Compilation.cs). The `using` here is a safety net
            // only, covering the case where the Compilation constructor itself
            // throws before `compilation` is assigned. MetadataLoadContext.Dispose()
            // is idempotent, so the resulting double-dispose on the success path is
            // intentional and safe.
            using var metadataLoadContext = new MetadataLoadContext(assemblyResolver, references.CoreAssemblyName);
            foreach (var reference in references.Compile)
            {
                // No try/catch: the de-dup invariant plus a ref-pack-only set
                // means every path here is a loadable managed assembly.
                metadataLoadContext.LoadFromAssemblyPath(reference.Path);
            }

            using var compilation = new Compilation(manifest.Name!, assemblyVersion, sources, metadataLoadContext);

            var diagnostics = compilation.GetDiagnostics().ToArray();
            foreach (var diagnostic in diagnostics)
            {
                Console.Error.WriteLine(FormatDiagnostic(diagnostic));
            }

            if (diagnostics.HasError())
            {
                return 1;
            }

            using var assemblyDefinition = compilation.Emit();
            Directory.CreateDirectory(outputDirectory);
            var assemblyPath = Path.Combine(outputDirectory, $"{manifest.Name}.dll");
            assemblyDefinition.Write(assemblyPath);
            WriteRuntimeConfig(outputDirectory, manifest.Name!, references.Framework);

            Console.WriteLine(assemblyPath);
            return 0;
        });
    }

    private static bool IsUnderDirectory(string filePath, string directory)
    {
        var fullFilePath = Path.GetFullPath(filePath);
        var fullDirectory = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        return fullFilePath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDiagnostic(Compiler.Diagnostics.Diagnostic diagnostic)
    {
        var position = diagnostic.TextLocation.GetStartLinePosition();
        var filePath = diagnostic.TextLocation.SourceText?.FilePath ?? "<unknown>";
        var level = diagnostic.Level == DiagnosticLevel.Error ? "error" : diagnostic.Level.ToString().ToLowerInvariant();
        return $"{filePath}({position.Line + 1},{position.Character + 1}): {level} {diagnostic.ErrorCode}: {diagnostic.Message}";
    }

    // No deps.json is written: its absence means the host's trusted-platform-
    // assemblies list is the output directory's contents, which is why an
    // empty RuntimeCopyLocal is fine for a package-free project.
    private static void WriteRuntimeConfig(string outputDirectory, string name, FrameworkReference framework)
        => File.WriteAllText(
            Path.Combine(outputDirectory, $"{name}.runtimeconfig.json"),
            $$"""
            {
              "runtimeOptions": {
                "tfm": "{{framework.Tfm}}",
                "framework": {
                  "name": "{{framework.Name}}",
                  "version": "{{framework.Version}}"
                }
              }
            }
            """);
}
