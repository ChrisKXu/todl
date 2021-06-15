using System;
using System.CommandLine;
using System.Threading.Tasks;
using Todl.Repl;

namespace Todl
{
    class Program
    {
        static Task<int> Main(string[] args) => ParseCommandLineOptions().InvokeAsync(args);

        static RootCommand ParseCommandLineOptions()
        {
            return new RootCommand("todl compiler")
            {
                new ReplCommand()
            };
        }
    }
}
