using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Todl.CommandLine.References;

/// <summary>
/// The result of building a referenced .csproj. Deliberately plain (no
/// <c>Microsoft.Build.*</c> types) — see <see cref="MSBuildBootstrap"/> for
/// why that matters for callers.
/// </summary>
internal sealed record ProjectBuildResult
{
    public required string TargetPath { get; init; }

    public required string OutputDirectory { get; init; }

    /// <summary>Global NuGet packages folder used for this project's restore, or null if unresolved.</summary>
    public string? NuGetPackageRoot { get; init; }
}

/// <summary>
/// Builds a referenced .csproj in-process via the MSBuild object model — no
/// <c>dotnet build</c>/<c>msbuild.exe</c> subprocess for the MSBuild engine
/// itself. <see cref="MSBuildBootstrap.EnsureRegistered"/> must have already
/// run before this type's methods are ever touched (see its doc comment for
/// why isolating this type is what makes that safe to guarantee).
/// </summary>
internal sealed class ProjectReferenceBuilder
{
    private const string Configuration = "Debug";

    public ProjectBuildResult Build(string csprojPath, string referenceName)
    {
        var globalProperties = new Dictionary<string, string>
        {
            ["Configuration"] = Configuration,
        };

        using var collection = new ProjectCollection(globalProperties);

        var evaluated = new Project(csprojPath, globalProperties, toolsVersion: null, collection);
        var targetFrameworks = evaluated.GetPropertyValue("TargetFrameworks");
        var targetFramework = evaluated.GetPropertyValue("TargetFramework");

        if (!string.IsNullOrWhiteSpace(targetFrameworks) && string.IsNullOrWhiteSpace(targetFramework))
        {
            throw new ProjectReferenceException(
                $"project reference '{referenceName}' ('{csprojPath}') multi-targets ({targetFrameworks}); " +
                "local project references to a multi-targeted project are not yet supported. " +
                "Give it a single 'TargetFramework' instead.");
        }

        var logger = new ErrorCollectingLogger();
        var parameters = new BuildParameters(collection)
        {
            Loggers = [logger],
        };

        BuildManager.DefaultBuildManager.BeginBuild(parameters);
        try
        {
            // Restore and Build are separate BuildRequests in one BeginBuild/
            // EndBuild session, against different global properties — matching
            // dotnet/sdk's own VirtualProjectBuildingCommand.
            var restoreGlobalProperties = new Dictionary<string, string>(globalProperties)
            {
                ["MSBuildRestoreSessionId"] = Guid.NewGuid().ToString("D"),
                ["MSBuildIsRestoring"] = "true",
            };
            var restoreInstance = new Project(csprojPath, restoreGlobalProperties, toolsVersion: null, collection).CreateProjectInstance();
            var restoreRequest = new BuildRequestData(restoreInstance, targetsToBuild: ["Restore"]);
            var restoreResult = BuildManager.DefaultBuildManager.BuildRequest(restoreRequest);

            if (restoreResult.OverallResult != BuildResultCode.Success)
            {
                throw new ProjectReferenceException(FormatFailure(referenceName, csprojPath, "restore", logger.Errors));
            }

            // Force it rather than depend on the SDK's unverified,
            // version-dependent default — closure discovery needs .deps.json.
            var buildGlobalProperties = new Dictionary<string, string>(globalProperties)
            {
                ["GenerateDependencyFile"] = "true",
            };
            var buildInstance = new Project(csprojPath, buildGlobalProperties, toolsVersion: null, collection).CreateProjectInstance();
            var buildRequest = new BuildRequestData(buildInstance, targetsToBuild: ["Build"]);
            var buildResult = BuildManager.DefaultBuildManager.BuildRequest(buildRequest);

            if (buildResult.OverallResult != BuildResultCode.Success)
            {
                throw new ProjectReferenceException(FormatFailure(referenceName, csprojPath, "build", logger.Errors));
            }

            var targetPath = buildInstance.GetPropertyValue("TargetPath");
            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
            {
                throw new ProjectReferenceException(
                    $"project reference '{referenceName}' ('{csprojPath}') built successfully but produced no usable 'TargetPath' output.");
            }

            var nuGetPackageRoot = buildInstance.GetPropertyValue("NuGetPackageRoot");

            return new ProjectBuildResult
            {
                TargetPath = targetPath,
                OutputDirectory = Path.GetDirectoryName(targetPath)!,
                NuGetPackageRoot = string.IsNullOrEmpty(nuGetPackageRoot) ? null : nuGetPackageRoot,
            };
        }
        finally
        {
            BuildManager.DefaultBuildManager.EndBuild();
        }
    }

    private static string FormatFailure(string referenceName, string csprojPath, string phase, IReadOnlyList<string> errors)
    {
        var details = errors.Count > 0
            ? string.Join(Environment.NewLine, errors.Select(e => "  " + e))
            : "  (no diagnostic details captured)";

        return $"{phase} failed for project reference '{referenceName}' ('{csprojPath}'):{Environment.NewLine}{details}";
    }

    // Collects errors instead of writing to the console — MSBuild's own log
    // output would otherwise be noise mixed into todl build's diagnostics.
    private sealed class ErrorCollectingLogger : ILogger
    {
        public List<string> Errors { get; } = [];

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Quiet;

        public string? Parameters { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ErrorRaised += (_, e) =>
                Errors.Add($"{e.File}({e.LineNumber},{e.ColumnNumber}): error {e.Code}: {e.Message}");
        }

        public void Shutdown()
        {
        }
    }
}
