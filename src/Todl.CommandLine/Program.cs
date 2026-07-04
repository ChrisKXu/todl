using System.CommandLine;
using Todl.CommandLine.Commands;

namespace Todl.CommandLine;

public static class Program
{
    public static int Run(string[] args)
    {
        var rootCommand = new RootCommand("The Todl compiler and build tool");

        rootCommand.Add(new BuildCommand());

        var parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }
}
