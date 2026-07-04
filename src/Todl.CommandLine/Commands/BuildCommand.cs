using System;
using System.CommandLine;

namespace Todl.CommandLine.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Build a Todl project")
    {
        var outputPathOption = new Option<string>("--output");
        outputPathOption.Aliases.Add("-o");

        var pathArgument = new Argument<string>("path");

        Add(outputPathOption);
        Add(pathArgument);

        SetAction((parseResult) =>
        {
            var path = parseResult.GetValue(pathArgument);
            var output = parseResult.GetValue(outputPathOption);

            Console.WriteLine($"stub: build {path}");
            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine($"output: {output}");
            }

            return 0;
        });
    }
}
