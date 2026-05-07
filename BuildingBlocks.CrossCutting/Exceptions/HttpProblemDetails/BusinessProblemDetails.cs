using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.CrossCutting.Exceptions.HttpProblemDetails;

/// <summary>
/// İş kuralı ihlalleri için RFC 7807 uyumlu ProblemDetails.
/// HTTP 400 Bad Request döner.
/// </summary>
public class BusinessProblemDetails : ProblemDetails
{
    public BusinessProblemDetails(string detail)
    {
        Title = "Business Rule Violation";
        Detail = detail;
        Status = StatusCodes.Status400BadRequest;
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    }
}
