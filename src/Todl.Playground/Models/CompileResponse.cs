using System;
using System.Collections.Generic;
using Todl.Compiler.Diagnostics;

namespace Todl.Playground.Models;

public record CompileResponse
(
    IEnumerable<Diagnostic> Diagnostics,
    Exception Exception,
    string DecompiledText
);
