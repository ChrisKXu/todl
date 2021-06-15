using System.Collections.Generic;

namespace Todl.Compiler.Evaluation
{
    public record EvaluatorResult
    {
        public IReadOnlyList<string> DiagnosticsOutput { get; init; }
        public string EvaluationOutput { get; init; }
    }
}
