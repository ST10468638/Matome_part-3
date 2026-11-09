using System.Net;

namespace RaceDay.Mvc.Services;

public sealed class ApiSessionCookieHandler : DelegatingHandler
{
    private const string SessionKey = "RaceDay.Api.SessionCookie";
    private const string CookieName = ".RaceDay.Session";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiSessionCookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await httpContext.Session.LoadAsync(cancellationToken);
            var sessionCookie = httpContext.Session.GetString(SessionKey);
            if (!string.IsNullOrWhiteSpace(sessionCookie))
            {
                request.Headers.Remove("Cookie");
                request.Headers.TryAddWithoutValidation("Cookie", sessionCookie);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (httpContext is not null &&
            response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            foreach (var header in cookieHeaders)
            {
                var cookiePair = header.Split(';', 2)[0].Trim();
                if (!cookiePair.StartsWith(CookieName + "=", StringComparison.Ordinal))
                {
                    continue;
                }

                if (cookiePair.Length == CookieName.Length + 1)
                {
                    httpContext.Session.Remove(SessionKey);
                }
                else
                {
                    httpContext.Session.SetString(SessionKey, cookiePair);
                }
            }
        }

        return response;
    }
}
