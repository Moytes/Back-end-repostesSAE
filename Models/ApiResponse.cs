namespace Back_end_RepostesSAE.Models;

/// <summary>
/// Envoltura de respuesta uniforme, compatible con el `ApiResponse` del frontend
/// (statusCode, message, data, intOpCode).
/// </summary>
public sealed class ApiResponse<T>
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public string? IntOpCode { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "OK", string intOpCode = "CL_200")
        => new() { StatusCode = 200, Message = message, Data = data, IntOpCode = intOpCode };

    public static ApiResponse<T> Created(T data, string message = "Created", string intOpCode = "CL_201")
        => new() { StatusCode = 201, Message = message, Data = data, IntOpCode = intOpCode };
}
