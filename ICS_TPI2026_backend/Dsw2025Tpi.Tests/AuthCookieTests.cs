using Dsw2025Tpi.Api.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Dsw2025Tpi.Tests;

public class AuthCookieTests
{
    [Fact]
    public void CreateOptions_EnDesarrollo_ActivaFlagsDeSeguridadYSameSiteStrict()
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var options = AuthCookieDefaults.CreateOptions(isDevelopment: true, expiresAt);

        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.Strict, options.SameSite);
        Assert.Equal("/", options.Path);
        Assert.NotNull(options.Expires);
        Assert.Equal(expiresAt, options.Expires.Value.UtcDateTime);
    }

    [Fact]
    public void CreateOptions_EnProduccion_UsaSameSiteNoneParaPermitirCrossSite()
    {
        var options = AuthCookieDefaults.CreateOptions(isDevelopment: false, DateTime.UtcNow.AddHours(1));

        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.None, options.SameSite);
    }

    [Fact]
    public void GetToken_LeeElJwtDeLaCookie()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{AuthCookieDefaults.TokenCookieName}=jwt-de-prueba";

        var token = AuthCookieDefaults.GetToken(context.Request);

        Assert.Equal("jwt-de-prueba", token);
    }

    [Fact]
    public void GetToken_SinCookie_DevuelveNull()
    {
        var context = new DefaultHttpContext();

        var token = AuthCookieDefaults.GetToken(context.Request);

        Assert.Null(token);
    }
}
