using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Evaluation;

namespace Todl.Repl
{
    sealed class ReplCommand : Command, ICommandHandler
    {
        private readonly Evaluator evaluator = new();

        public ReplCommand()
            : base("repl", "Interactive Shell")
        {
            TreatUnmatchedTokensAsErrors = true;
            Handler = this;
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    Console.Write("> ");

                    var input = Console.ReadLine()!;

                    if (input.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return 0;
                    }

                    var evaluatorResult = this.evaluator.Evaluate(SourceText.FromString(input));

                    WriteEvaluationResult(evaluatorResult);
                }
            });
        }

        private static void WriteEvaluationResult(EvaluatorResult evaluatorResult)
        {
            if (evaluatorResult.DiagnosticsOutput.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                foreach (var line in evaluatorResult.DiagnosticsOutput)
                {
                    Console.WriteLine($"  {line}");
                }

                Console.ResetColor();
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Green;

            var output = evaluatorResult.EvaluationOutput;
            if (evaluatorResult.ResultType.Equals(TypeSymbol.ClrString))
            {
                output = $"\"{output}\"";
            }

            Console.WriteLine($"==> {output}");
            Console.ResetColor();
        }
    }
}
