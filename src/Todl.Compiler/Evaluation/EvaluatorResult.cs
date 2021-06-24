using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.Evaluation
{
    public record EvaluatorResult
    {
        public IReadOnlyList<string> DiagnosticsOutput { get; init; }
        public object EvaluationOutput { get; init; }
        public TypeSymbol ResultType { get; init; }
    }
}
