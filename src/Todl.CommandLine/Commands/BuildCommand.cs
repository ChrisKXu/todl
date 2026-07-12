using System;
using System.CommandLine;
using Todl.CommandLine.Manifest;

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
            var output = parseResult.GetValue(outputPathOption);

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

            Console.WriteLine($"stub: build {manifest.Name} v{manifest.Version} from {path}");
            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine($"output: {output}");
            }

            return 0;
        });
    }
}
