using System;

namespace Todl.CommandLine.Manifest;

// Message is meant to be surfaced directly to the user as an actionable
// diagnostic, never as a raw stack trace.
public sealed class TodlManifestException : Exception
{
    public TodlManifestException(string message) : base(message)
    {
    }

    public TodlManifestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
