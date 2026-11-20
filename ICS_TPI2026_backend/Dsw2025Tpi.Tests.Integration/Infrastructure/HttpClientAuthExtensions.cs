using System.Net.Http.Headers;

namespace Dsw2025Tpi.Tests.Integration.Infrastructure;

public static class HttpClientAuthExtensions
{
    /// <summary>Agrega "Authorization: Bearer {token}" a todas las peticiones del cliente.</summary>
    public static HttpClient WithAuth(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
