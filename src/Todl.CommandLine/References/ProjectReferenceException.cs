using System;

namespace Todl.CommandLine.References;

// Message is meant to be surfaced directly to the user as an actionable
// diagnostic, never as a raw stack trace — mirrors TodlManifestException.
public sealed class ProjectReferenceException : Exception
{
    public ProjectReferenceException(string message) : base(message)
    {
    }

    public ProjectReferenceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
