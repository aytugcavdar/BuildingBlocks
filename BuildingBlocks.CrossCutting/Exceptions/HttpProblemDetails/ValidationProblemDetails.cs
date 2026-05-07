using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.CrossCutting.Exceptions.HttpProblemDetails;

/// <summary>
/// FluentValidation hataları için RFC 7807 uyumlu ProblemDetails.
/// HTTP 422 Unprocessable Entity döner.
/// </summary>
public class ValidationProblemDetails : ProblemDetails
{
    public List<string> Errors { get; set; }

    public ValidationProblemDetails(List<string> errors)
    {
        Title = "Validation Error";
        Detail = "One or more validation errors occurred.";
        Status = StatusCodes.Status422UnprocessableEntity;
        Type = "https://tools.ietf.org/html/rfc4918#section-11.2";
        Errors = errors;
    }
}