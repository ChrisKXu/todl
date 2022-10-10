using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Todl.Playground.Models;

namespace Todl.Playground.Controllers;

public class ExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = new JsonResult(new ErrorResponse(context.Exception))
        {
            StatusCode = 500
        };
    }
}
