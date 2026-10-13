using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PharmacyWorkflow.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "PharmacyWorkflow";
    public string Audience { get; set; } = "PharmacyWorkflow.Client";

    /// <summary>서명 키 (32자 이상). 운영에서는 환경 변수(Jwt__Key)나 비밀 저장소로 주입합니다.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpireMinutes { get; set; } = 60;
}

/// <summary>사용자 정보로 JWT 액세스 토큰을 발급합니다.</summary>
public sealed class TokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);
    }

    public (string Token, DateTimeOffset ExpiresAt) CreateToken(AppUser user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.ExpireMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            // 토큰에 담는 정보: 사용자 ID(sub), 표시 이름(name), 역할(role)
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id),
                new Claim("name", user.DisplayName),
                new Claim("role", user.Role),
            }),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _credentials,
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, new DateTimeOffset(expires, TimeSpan.Zero));
    }
}
