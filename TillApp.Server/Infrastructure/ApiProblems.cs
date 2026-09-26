using Microsoft.AspNetCore.Mvc;

namespace TillApp.Server.Infrastructure;

public static class ApiProblems
{
    public static NotFoundObjectResult NotFound(HttpContext httpContext, string resourceName)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found",
            Detail = $"The requested {resourceName} could not be found."
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return new NotFoundObjectResult(problem);
    }
}
