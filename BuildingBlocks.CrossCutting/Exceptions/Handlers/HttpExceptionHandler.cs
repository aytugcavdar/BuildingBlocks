using BuildingBlocks.CrossCutting.Exceptions.HttpProblemDetails;
using BuildingBlocks.CrossCutting.Exceptions.Types;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.CrossCutting.Exceptions.Handlers;

/// <summary>
/// Exception'ları HTTP response'a dönüştüren handler.
/// ExceptionMiddleware tarafından kullanılır.
/// </summary>
public class HttpExceptionHandler
{
    private HttpResponse? _response;

    public HttpResponse Response
    {
        get => _response ?? throw new ArgumentNullException(nameof(_response));
        set => _response = value;
    }

    public Task HandleExceptionAsync(Exception exception)
    {
        Response.ContentType = "application/json";

        return exception switch
        {
            // EntityNotFoundException önce gelmeli (NotFoundException'dan türüyor)
            EntityNotFoundException entityNotFoundException => HandleException(entityNotFoundException),
            BusinessValidationException validationException => HandleException(validationException),
            BusinessException businessException             => HandleException(businessException),
            NotFoundException notFoundException             => HandleException(notFoundException),
            _                                              => HandleException(exception)
        };
    }

    private Task HandleException(BusinessException businessException)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return Response.WriteAsJsonAsync(new BusinessProblemDetails(businessException.Message));
    }

    private Task HandleException(BusinessValidationException validationException)
    {
        Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return Response.WriteAsJsonAsync(new ValidationProblemDetails(validationException.Errors));
    }

    private Task HandleException(NotFoundException notFoundException)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return Response.WriteAsJsonAsync(new NotFoundProblemDetails(notFoundException.Message));
    }

    private Task HandleException(EntityNotFoundException entityNotFoundException)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return Response.WriteAsJsonAsync(new NotFoundProblemDetails(entityNotFoundException.Message));
    }

    private Task HandleException(Exception exception)
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return Response.WriteAsJsonAsync(new InternalServerErrorProblemDetails());
    }
}