using Microsoft.AspNetCore.Http;

namespace Dsw2025Tpi.Api.Authentication;

public static class AuthCookieDefaults
{
    public const string TokenCookieName = "token";

    /// <summary>
    /// Opciones de la cookie que transporta el JWT.
    /// En produccion el frontend (Vercel) y la API (Azure) son cross-site, por lo que
    /// SameSite=Strict impediria que el navegador envie la cookie; en desarrollo se usa Strict.
    /// </summary>
    public static CookieOptions CreateOptions(bool isDevelopment, DateTime? expiresAt = null)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = isDevelopment ? SameSiteMode.Strict : SameSiteMode.None,
            Path = "/",
            Expires = expiresAt
        };
    }

    public static string? GetToken(HttpRequest request)
    {
        return request.Cookies.TryGetValue(TokenCookieName, out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;
    }
}
