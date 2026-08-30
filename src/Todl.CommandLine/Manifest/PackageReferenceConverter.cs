using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Versioning;

namespace Todl.CommandLine.Manifest;

public sealed class PackageReferenceConverter : JsonConverter<PackageReference>
{
    public override PackageReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString() ?? string.Empty;

            // Disambiguation is a strict function of the exact grammar NuGet
            // package references already depend on, not a suffix/separator
            // heuristic: a string that parses as a NuGet version requirement
            // is a NuGet reference, anything else is a relative folder path
            // to a local project. A leading "./" never parses as a version,
            // so it's the escape hatch for a path that would otherwise
            // collide with version syntax (e.g. a sibling folder literally
            // named "2.0").
            return VersionRange.TryParse(value, out _)
                ? new PackageReference { Version = value }
                : new PackageReference { Path = value };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            var hasPath = root.TryGetProperty("path", out var pathElement);
            var hasVersion = root.TryGetProperty("version", out var versionElement);

            if (hasPath && hasVersion)
            {
                throw new JsonException("A package reference entry given as an object must specify exactly one of 'path' or 'version', not both.");
            }

            if (hasPath)
            {
                var path = pathElement.GetString();
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new JsonException("A package reference entry given as an object with a 'path' must have a non-empty value.");
                }

                return new PackageReference { Path = path };
            }

            if (hasVersion)
            {
                var version = versionElement.GetString();
                var source = root.TryGetProperty("source", out var sourceElement)
                    ? sourceElement.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(version))
                {
                    throw new JsonException("A NuGet package entry given as an object must specify a non-empty 'version'.");
                }

                return new PackageReference { Version = version, Source = source };
            }

            throw new JsonException("A package reference entry given as an object must specify exactly one of 'path' or 'version'.");
        }

        throw new JsonException(
            $"Unexpected token '{reader.TokenType}' for a package reference entry; expected a version/path string or an object with a 'version' or 'path' field.");
    }

    public override void Write(Utf8JsonWriter writer, PackageReference value, JsonSerializerOptions options)
    {
        if (value.Path is not null)
        {
            writer.WriteStringValue(value.Path);
            return;
        }

        if (string.IsNullOrEmpty(value.Source))
        {
            writer.WriteStringValue(value.Version);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("version", value.Version);
        writer.WriteString("source", value.Source);
        writer.WriteEndObject();
    }
}
