namespace TeamFlow.Infrastructure.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "TeamFlow";
    public string Audience { get; set; } = "TeamFlow";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
