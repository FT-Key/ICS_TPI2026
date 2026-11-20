using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Dsw2025Tpi.Tests.Integration.Infrastructure;

/// <summary>
/// Generador de JWT de prueba con los mismos claims que emite JwtTokenService
/// (sub, jti, role e "id") y el issuer, audience y clave que el factory configura en la API.
/// La clave es exclusiva de las pruebas: no es un secreto real.
/// </summary>
public static class TestJwtTokens
{
    public const string Issuer = "Dsw2025Tpi.Api";
    public const string Audience = "Dsw2025Tpi.Client";
    public const string SigningKey = "clave-de-pruebas-de-integracion-tp4-no-es-un-secreto-real-0123456789";

    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    public static string CreateToken(string role, Guid? userId = null, string userName = "tester", TimeSpan? lifetime = null)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("id", (userId ?? Guid.NewGuid()).ToString())
        };

        var now = DateTime.UtcNow;
        var expires = now.Add(lifetime ?? TimeSpan.FromMinutes(30));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: expires < now ? expires.AddMinutes(-1) : now,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string Admin(Guid? userId = null) => CreateToken(AdminRole, userId, "admin");

    public static string User(Guid? userId = null) => CreateToken(UserRole, userId, "cliente");
}
