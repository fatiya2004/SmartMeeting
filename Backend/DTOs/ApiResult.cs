namespace SmartMeeting.Api.DTOs;

/// <summary>
/// Résultat standard renvoyé par les services métier au controller.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ServiceResult<T> Fail(string error) => new() { Success = false, Error = error };
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;

    public MessageResponse() { }

    public MessageResponse(string message) => Message = message;
}
