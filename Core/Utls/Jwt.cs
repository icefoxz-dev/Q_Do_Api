using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OrderDbLib.Entities;

namespace OrderApiFun.Core.Utls;

public static class Jwt
{
    private const string JwtSecret = "www.icefoxz.com/OrderWebService";
    private const string Issuer = "www.icefoxz.com";
    private const string Audience = "icefoxzApp";
    private static byte[] KeyInBytes { get; } = Encoding.ASCII.GetBytes(JwtSecret);

    private static SymmetricSecurityKey SymmetricSecurityKey { get; } = new SymmetricSecurityKey(KeyInBytes);
    public static string GenerateToken(User user, int jwtExpHours)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddHours(jwtExpHours),
            SigningCredentials = new SigningCredentials(SymmetricSecurityKey,
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static ClaimsPrincipal ValidateToken(string token, ILogger log)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SymmetricSecurityKey,
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var _);
            return principal;
        }
        catch (Exception e)
        {
            log.LogInformation(e.Message);
            return null;
        }
    }
}