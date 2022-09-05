using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Todl.Playground.Functions;

public class InfoFunction
{
    [FunctionName("info")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest request,
        ILogger logger)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");

        return new OkObjectResult(new { Status = "Running" });
    }
}
