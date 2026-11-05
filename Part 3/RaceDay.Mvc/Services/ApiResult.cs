using System.Net;

namespace RaceDay.Mvc.Services;

public sealed record ApiResult<T>(bool Succeeded, T? Data, string? Error, HttpStatusCode StatusCode);

public sealed record ApiFileResult(byte[] Bytes, string ContentType);
