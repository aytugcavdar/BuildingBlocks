using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.CrossCutting.Exceptions.HttpProblemDetails;

/// <summary>
/// 500 Internal Server Error için RFC 7807 uyumlu ProblemDetails.
/// Gerçek hata detayı güvenlik gerekçesiyle gizlenir.
/// </summary>
public class InternalServerErrorProblemDetails : ProblemDetails
{
    public InternalServerErrorProblemDetails()
    {
        Title = "Internal Server Error";
        Detail = "An unexpected error occurred. Please contact support if the problem persists.";
        Status = StatusCodes.Status500InternalServerError;
        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
    }
}
