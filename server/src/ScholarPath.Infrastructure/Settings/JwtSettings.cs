using System.ComponentModel.DataAnnotations;

namespace ScholarPath.Infrastructure.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32)]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
