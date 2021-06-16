﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Evaluation;

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

                    var evaluator = new Evaluator(SourceText.FromString(input));
                    var evaluatorResult = evaluator.Evaluate();

                    WriteEvaluationResult(evaluatorResult);
                }
            });
        }

        private void WriteEvaluationResult(EvaluatorResult evaluatorResult)
        {
            var originalColor = Console.ForegroundColor;

            if (evaluatorResult.DiagnosticsOutput.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                foreach (var line in evaluatorResult.DiagnosticsOutput)
                {
                    Console.WriteLine($"  {line}");
                }

                Console.ForegroundColor = originalColor;
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"==> {evaluatorResult.EvaluationOutput}");
            Console.ForegroundColor = originalColor;
        }
    }
}