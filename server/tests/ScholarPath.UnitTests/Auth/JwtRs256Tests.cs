using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using ScholarPath.Infrastructure.Settings;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

/// <summary>
/// Covers the RS256 JWT migration: the local key provider's two modes
/// (ephemeral key vs. PEM file) and an end-to-end token issue/validate round
/// trip through <see cref="TokenService"/>.
/// </summary>
public sealed class JwtRs256Tests
{
    private static JwtOptions BaseOptions() => new()
    {
        Issuer = "https://scholarpath.local",
        Audience = "https://scholarpath.local",
        AccessTokenExpirationMinutes = 60,
    };

    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ApplicationUser TestUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "rs256@test.local",
        FirstName = "Rs",
        LastName = "Tester",
        AccountStatus = AccountStatus.Active,
    };

    [Fact]
    public void LocalProvider_NoKeyPath_GeneratesEphemeralKey_AndFlagsWarning()
    {
        using var provider = new LocalJwtKeyProvider(Options.Create(BaseOptions()));

        var description = provider.Describe(out var isWarning);

        isWarning.Should().BeTrue("an ephemeral dev key must not reach production");
        description.Should().Contain("ephemeral");
        provider.GetSigningKey().Should().NotBeNull();
        // Signing and validation must be the same RSA key instance.
        provider.GetValidationKey().Should().BeSameAs(provider.GetSigningKey());
        provider.KeyId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void LocalProvider_WithPemFile_LoadsThatKey_WithoutWarning()
    {
        var pemPath = Path.Combine(Path.GetTempPath(), $"jwt-test-{Guid.NewGuid():N}.pem");
        using (var rsa = RSA.Create(2048))
        {
            File.WriteAllText(pemPath, rsa.ExportRSAPrivateKeyPem());
        }

        try
        {
            var opts = BaseOptions();
            opts.DevKeyPath = pemPath;

            using var provider = new LocalJwtKeyProvider(Options.Create(opts));
            var description = provider.Describe(out var isWarning);

            isWarning.Should().BeFalse();
            description.Should().Contain(pemPath);
            provider.GetSigningKey().Rsa.KeySize.Should().Be(2048);
        }
        finally
        {
            File.Delete(pemPath);
        }
    }

    [Fact]
    public void TokenService_IssuesRs256Token_ValidatedByPublicKey()
    {
        using var keyProvider = new LocalJwtKeyProvider(Options.Create(BaseOptions()));
        var opts = BaseOptions();
        using var db = CreateDb();
        var clock = Substitute.For<IDateTimeService>();
        // Use the real "now" — the token is validated below with
        // ValidateLifetime=true against the wall clock, so a hard-coded date
        // would make this test a time-bomb.
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var sut = new TokenService(
            Options.Create(opts),
            keyProvider,
            db,
            clock,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TokenService>.Instance);

        var pair = sut.IssueTokens(TestUser(), ["Student"], "Student", rememberMe: false);

        // The token must be signed with RS256.
        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(pair.AccessToken);
        parsed.Header.Alg.Should().Be(SecurityAlgorithms.RsaSha256);

        // It must validate against the provider's public key.
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = opts.Issuer,
            ValidAudience = opts.Audience,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            IssuerSigningKey = keyProvider.GetValidationKey(),
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        var principal = handler.ValidateToken(pair.AccessToken, validationParameters, out _);
        principal.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void Rs256Token_FailsValidation_WhenSignedByADifferentKey()
    {
        using var issuerProvider = new LocalJwtKeyProvider(Options.Create(BaseOptions()));
        using var attackerProvider = new LocalJwtKeyProvider(Options.Create(BaseOptions()));
        var opts = BaseOptions();
        using var db = CreateDb();
        var clock = Substitute.For<IDateTimeService>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var sut = new TokenService(
            Options.Create(opts),
            issuerProvider,
            db,
            clock,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TokenService>.Instance);

        var pair = sut.IssueTokens(TestUser(), ["Student"], "Student", rememberMe: false);

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = opts.Issuer,
            ValidAudience = opts.Audience,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
            // A different RSA key — the signature must not verify.
            IssuerSigningKey = attackerProvider.GetValidationKey(),
        };

        var act = () => handler.ValidateToken(pair.AccessToken, validationParameters, out _);
        act.Should().Throw<SecurityTokenException>();
    }
}
