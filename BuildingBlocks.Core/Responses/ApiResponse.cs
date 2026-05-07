namespace BuildingBlocks.Core.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public int StatusCode { get; set; }

    public ApiResponse()
    {
        Errors = new List<string>();
    }

    public ApiResponse(T data, string? message = null)
    {
        Success = true;
        Data = data;
        Message = message;
        StatusCode = 200;
        Errors = new List<string>();
    }

    public ApiResponse(string message, int statusCode = 400)
    {
        Success = false;
        Message = message;
        StatusCode = statusCode;
        Errors = new List<string>();
    }

    public ApiResponse(List<string> errors, int statusCode = 400)
    {
        Success = false;
        Errors = errors;
        StatusCode = statusCode;
    }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>(data, message);
    }

    public static ApiResponse<T> FailResult(string message, int statusCode = 400)
    {
        return new ApiResponse<T>(message, statusCode);
    }

    public static ApiResponse<T> FailResult(List<string> errors, int statusCode = 400)
    {
        return new ApiResponse<T>(errors, statusCode);
    }
}