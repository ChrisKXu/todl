using System;

namespace Todl.Playground.Models;

public class ErrorResponse
{
    public string Message { get; }
    public string StackTrace { get; }

    public ErrorResponse(Exception exception)
    {
        Message = exception.Message;
        StackTrace = exception.StackTrace;
    }
}
