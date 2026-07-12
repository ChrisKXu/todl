using System;
using System.IO;

namespace Todl.CommandLine.Tests;

internal static class TestPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string SamplesDirectory => Path.Combine(RepoRoot, "samples");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException($"Could not locate the repo root (global.json) above '{AppContext.BaseDirectory}'.");
        }

        return directory.FullName;
    }
}
