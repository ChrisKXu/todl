using System;
using Microsoft.Build.Locator;

namespace Todl.CommandLine.References;

/// <summary>
/// Registers the machine's installed .NET SDK with Microsoft.Build.Locator
/// before any <c>Microsoft.Build.*</c> type is touched — required because the
/// JIT resolves a method's type tokens (including <c>Microsoft.Build.*</c>
/// ones) before its first statement runs. Safe to call from anywhere: this
/// type and <see cref="ProjectReferenceBuilder"/> both keep
/// <c>Microsoft.Build.*</c> types out of their public surface, so calling
/// <see cref="EnsureRegistered"/> and then using
/// <see cref="ProjectReferenceBuilder"/> from the same method never resolves
/// those tokens until <see cref="ProjectReferenceBuilder"/>'s own methods are
/// first invoked, by which point registration has already run.
/// </summary>
internal static class MSBuildBootstrap
{
    private static readonly object lockObject = new();
    private static bool registered;

    public static void EnsureRegistered()
    {
        if (registered)
        {
            return;
        }

        lock (lockObject)
        {
            if (registered)
            {
                return;
            }

            if (!MSBuildLocator.IsRegistered)
            {
                try
                {
                    // Also sets the MSBuildExtensionsPath/MSBuildSDKsPath env
                    // vars the SDK resolver needs (verified against
                    // Microsoft.Build.Locator's source) — no manual setup needed.
                    MSBuildLocator.RegisterDefaults();
                }
                catch (InvalidOperationException ex)
                {
                    throw new ProjectReferenceException(
                        "error: no .NET SDK found to build local project references. " +
                        "Local project references require a full .NET SDK install (not just a runtime) " +
                        $"on the machine running todl. ({ex.Message})",
                        ex);
                }
            }

            registered = true;
        }
    }
}
