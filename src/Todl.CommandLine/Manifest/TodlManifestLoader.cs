using System;
using System.IO;
using System.Text.Json;

namespace Todl.CommandLine.Manifest;

public static class TodlManifestLoader
{
    public const string ManifestFileName = "todl.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static TodlManifest Load(string? path)
    {
        var manifestPath = ResolveManifestPath(path);

        if (!File.Exists(manifestPath))
        {
            throw new TodlManifestException($"Could not find a Todl project manifest at '{manifestPath}'.");
        }

        string json;
        try
        {
            json = File.ReadAllText(manifestPath);
        }
        catch (IOException ex)
        {
            throw new TodlManifestException($"Failed to read '{manifestPath}': {ex.Message}", ex);
        }

        return Parse(json, manifestPath);
    }

    public static TodlManifest Parse(string json, string? sourcePath = null)
    {
        var displayPath = sourcePath ?? ManifestFileName;

        TodlManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<TodlManifest>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new TodlManifestException($"'{displayPath}' contains malformed JSON: {ex.Message}", ex);
        }

        if (manifest is null)
        {
            throw new TodlManifestException($"'{displayPath}' is empty or invalid.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            throw new TodlManifestException($"'{displayPath}' must specify a non-empty 'name'.");
        }

        return manifest;
    }

    // Given a path (defaulting to the current directory), resolves the todl.json
    // manifest whether the path points at a directory or at the manifest file itself.
    public static string ResolveManifestPath(string? path)
    {
        var resolvedPath = Path.GetFullPath(string.IsNullOrWhiteSpace(path) ? Environment.CurrentDirectory : path);

        return Directory.Exists(resolvedPath)
            ? Path.Combine(resolvedPath, ManifestFileName)
            : resolvedPath;
    }
}
