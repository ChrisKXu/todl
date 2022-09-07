using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Todl.Playground.Functions;

public class CompileFunction
{
    [FunctionName("compile")]
    public Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
        ILogger logger)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");

        return Task.FromResult<IActionResult>(new OkObjectResult(new { Status = "Running" }));
    }
}
