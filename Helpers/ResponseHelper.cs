namespace Helpers.ResponseHelper;

public static class ResponseHelper
{
    public static bool IsResponseOK(int statusCode)
    {
        return statusCode >= 200 && statusCode <= 299;
    }
    public static IResult ResponseStatus(string message, int statusCode)
    {
        string statusMessage = IsResponseOK(statusCode) 
            ? $"Success: {message}" 
            : $"Error: {message}";
        
        return Results.Json(new { message = statusMessage }, statusCode: statusCode);
    }
}