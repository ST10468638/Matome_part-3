using System.Net;

namespace RaceDay.Mvc.Services;

public sealed class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
}
