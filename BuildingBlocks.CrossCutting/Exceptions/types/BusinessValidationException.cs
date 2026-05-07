using FluentValidation.Results;

namespace BuildingBlocks.CrossCutting.Exceptions.Types;

/// <summary>
/// FluentValidation hatalarını taşıyan exception.
/// HTTP 422 Unprocessable Entity döner.
/// </summary>
public class BusinessValidationException : Exception
{
    public List<string> Errors { get; }

    public BusinessValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new List<string>();
    }

    public BusinessValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures.Select(f => f.ErrorMessage).ToList();
    }

    public BusinessValidationException(string message) : base(message)
    {
        Errors = new List<string> { message };
    }

    public BusinessValidationException(List<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }
}
