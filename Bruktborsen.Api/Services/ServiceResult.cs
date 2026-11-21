namespace Bruktborsen.Api.Services;

public enum ServiceError
{
    None,
    NotFound,       // 404
    BadRequest,     // 400
    Forbidden,      // 403
    Conflict        // 409
}

public class ServiceResult
{
    public ServiceError Error { get; init; } = ServiceError.None;
    public string? Message { get; init; }
    public bool IsSuccess => Error == ServiceError.None;

    public static ServiceResult Ok() => new();
    public static ServiceResult Fail(ServiceError error, string message) =>
        new() { Error = error, Message = message };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }

    public static ServiceResult<T> Ok(T value) => new() { Value = value };
    public new static ServiceResult<T> Fail(ServiceError error, string message) =>
        new() { Error = error, Message = message };
}