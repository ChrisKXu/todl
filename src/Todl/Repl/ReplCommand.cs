using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todl.Repl
{
    sealed class ReplCommand : Command, ICommandHandler
    {
        public ReplCommand()
            : base ("repl", "Interactive Shell")
        {
            TreatUnmatchedTokensAsErrors = true;
            Handler = this;
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            Console.WriteLine(">");

            return Task.FromResult(0);
        }
    }
}
