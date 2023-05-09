using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Azure.Identity;
using Microsoft.IdentityModel.Tokens;
using OrderApiFun.Core.Tokens;
using OrderDbLib.Entities;

namespace OrderApiFun.Core.Services;

public class JwtTokenService
{
    private const string JwtKey = "www.icefoxz.com/OrderWebService";
    private const string JwtIssuer = "www.icefoxz.com";
    private const string JwtAudience = "icefoxzApp";
    public const string ProviderName = "Jwt";
    public const string TokenType = "token_type";
    public const string RefreshTokenHeader = "refresh_token";
    public const string AccessTokenHeader = "access_token";
    private static byte[] KeyInBytes { get; } = Encoding.ASCII.GetBytes(JwtKey);

    private SymmetricSecurityKey SymmetricSecurityKey { get; } = new SymmetricSecurityKey(KeyInBytes);
    public JwtSecurityTokenHandler JwtSecurityTokenHandler { get; } = new JwtSecurityTokenHandler();
    private TokenValidationParameters TokenValidationParameters
    {
        get
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SymmetricSecurityKey,
                ValidateIssuer = true,
                ValidIssuer = JwtIssuer,
                ValidateAudience = true,
                ValidAudience = JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            return validationParameters;
        }
    }

    public string GenerateRefreshToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.UserName),//Refresh Token记录的是Username
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(TokenType, RefreshTokenHeader),
        };
        var refreshToken = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(30),
            SigningCredentials = new SigningCredentials(SymmetricSecurityKey, SecurityAlgorithms.HmacSha256Signature)
        };
        var token = JwtSecurityTokenHandler.CreateToken(refreshToken);
        return JwtSecurityTokenHandler.WriteToken(token);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token, string username)
    {
        try
        {
            var result = await ValidateTokenAsync(token);
            if (result.Result != TokenValidation.Results.Valid) return false;
            var principal = result.Principal;
            if (!principal.HasClaim(TokenType, RefreshTokenHeader))
                throw new AuthenticationFailedException($"Token type not matched! {principal}");
            return username == result.Principal.Identity?.Name;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public string GenerateAccessToken(User user, params Claim[] additionalClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Id),//注意accessToken记录的是userId而非username
            new(TokenType, AccessTokenHeader),
            // ... other claims ...
        };
        claims.AddRange(additionalClaims);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            Expires = DateTime.UtcNow.AddHours(3),
            SigningCredentials = new SigningCredentials(SymmetricSecurityKey, SecurityAlgorithms.HmacSha256Signature)
        };
        var token = JwtSecurityTokenHandler.CreateToken(tokenDescriptor);
        return JwtSecurityTokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Token基础验证, 包括正确的issuer,audience,过期问题
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public Task<TokenValidation> ValidateTokenAsync(string accessToken)
    {
        try
        {
            var principal = JwtSecurityTokenHandler.ValidateToken(accessToken, TokenValidationParameters, out _);
            // 如果验证成功，返回有效的 TokenValidationResult
            return Task.FromResult(TokenValidation.Valid(principal));
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult(TokenValidation.Expired());
        }
        catch (Exception e)
        {
            return Task.FromResult(TokenValidation.Error(e.Message));
        }
    }
}