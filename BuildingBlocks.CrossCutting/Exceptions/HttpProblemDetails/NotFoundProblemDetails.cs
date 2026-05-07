using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.CrossCutting.Exceptions.HttpProblemDetails;

/// <summary>
/// 404 Not Found hataları için RFC 7807 uyumlu ProblemDetails.
/// </summary>
public class NotFoundProblemDetails : ProblemDetails
{
    public NotFoundProblemDetails(string detail)
    {
        Title = "Resource Not Found";
        Detail = detail;
        Status = StatusCodes.Status404NotFound;
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
    }
}
