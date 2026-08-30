using System;
using Microsoft.Build.Locator;

namespace Todl.CommandLine.References;

/// <summary>
/// Registers the machine's installed .NET SDK with Microsoft.Build.Locator
/// before any <c>Microsoft.Build.*</c> type is touched.
///
/// This type deliberately references only <see cref="MSBuildLocator"/>
/// (a separate package that is safe to touch before registration) and never
/// a <c>Microsoft.Build.*</c> SDK type. The actual MSBuild hosting work lives
/// in <see cref="ProjectReferenceBuilder"/>, a different class whose public
/// surface also uses no <c>Microsoft.Build.*</c> types — the CLR resolves a
/// call's target-method *signature* eagerly but defers JITting the target
/// method's own body (and therefore resolving the types *it* references)
/// until the method is first invoked. Since neither this type's nor
/// <see cref="ProjectReferenceBuilder"/>'s public signatures mention a
/// <c>Microsoft.Build.*</c> type, calling <see cref="EnsureRegistered"/> and
/// then constructing a <see cref="ProjectReferenceBuilder"/> from the same
/// caller method is safe — <c>Microsoft.Build.*</c> type tokens are resolved
/// only once <see cref="ProjectReferenceBuilder"/>'s own methods are JITted,
/// which happens strictly after this method has returned.
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
                    // RegisterDefaults() both hooks assembly resolution to the
                    // discovered SDK and sets the MSBuildExtensionsPath/MSBuildSDKsPath
                    // environment variables its SDK resolver needs to find
                    // Sdk.props/Sdk.targets for <Project Sdk="Microsoft.NET.Sdk">
                    // (RegisterInstance calls ApplyDotNetSdkEnvironmentVariables
                    // whenever the discovered instance is DiscoveryType.DotNetSdk,
                    // the only kind GetInstances() can return on .NET as opposed to
                    // .NET Framework — verified against Microsoft.Build.Locator's
                    // own source). No manual env var setup needed here.
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
