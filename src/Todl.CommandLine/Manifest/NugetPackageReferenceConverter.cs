using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Todl.CommandLine.Manifest;

public sealed class NugetPackageReferenceConverter : JsonConverter<NugetPackageReference>
{
    public override NugetPackageReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var version = reader.GetString();
            return new NugetPackageReference { Version = version ?? string.Empty };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            var version = root.TryGetProperty("version", out var versionElement)
                ? versionElement.GetString()
                : null;
            var source = root.TryGetProperty("source", out var sourceElement)
                ? sourceElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new JsonException("A NuGet package entry given as an object must specify a non-empty 'version'.");
            }

            return new NugetPackageReference { Version = version, Source = source };
        }

        throw new JsonException(
            $"Unexpected token '{reader.TokenType}' for a NuGet package entry; expected a version string or an object with a 'version' field.");
    }

    public override void Write(Utf8JsonWriter writer, NugetPackageReference value, JsonSerializerOptions options)
    {
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
