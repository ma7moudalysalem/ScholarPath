using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Services;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.UnitTests.Security;

/// <summary>
/// Application-level encryption of sensitive database columns at rest (SRS
/// security NFR): the AES-256-GCM crypto service and its key providers.
/// </summary>
public sealed class FieldEncryptionTests
{
    // A fixed, well-known 256-bit key — Base64 of 32 bytes. Tests must be
    // deterministic, so they never generate the key randomly.
    private const string DevKeyBase64 = "vY2z2EgwRy2+Ls92notyOeyo5HuMEodhzhytZm81KFg=";

    private static IFieldEncryptionService Service(string? key = DevKeyBase64)
    {
        var provider = new LocalFieldEncryptionKeyProvider(
            Options.Create(new FieldEncryptionOptions { DevKey = key }));
        return new AesGcmFieldEncryptionService(provider);
    }

    // ─── Round trip ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("a short note")]
    [InlineData("")]
    [InlineData("Multi-line\r\nbiography with punctuation: !@#$%^&*()")]
    [InlineData("Unicode — Arabic نص حساس and emoji \U0001F510")]
    public void Encrypt_then_Decrypt_round_trips_the_original_plaintext(string plaintext)
    {
        var sut = Service();

        var cipher = sut.Encrypt(plaintext);
        var roundTripped = sut.Decrypt(cipher);

        roundTripped.Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_and_Decrypt_map_null_to_null()
    {
        var sut = Service();

        sut.Encrypt(null).Should().BeNull();
        sut.Decrypt(null).Should().BeNull();
    }

    // ─── Ciphertext shape ────────────────────────────────────────────────────

    [Fact]
    public void Ciphertext_differs_from_plaintext_and_carries_the_v1_prefix()
    {
        var sut = Service();
        const string plaintext = "sensitive personal information";

        var cipher = sut.Encrypt(plaintext);

        cipher.Should().NotBeNull();
        cipher.Should().StartWith("enc:v1:");
        cipher.Should().NotBe(plaintext);
        cipher.Should().NotContain(plaintext, "the plaintext must not be visible in the stored value");
    }

    [Fact]
    public void Two_encryptions_of_the_same_plaintext_differ_because_the_nonce_is_random()
    {
        var sut = Service();
        const string plaintext = "same input every time";

        var first = sut.Encrypt(plaintext);
        var second = sut.Encrypt(plaintext);

        first.Should().NotBe(second, "a fresh random nonce per call must yield different ciphertext");
        // …yet both still decrypt back to the same plaintext.
        sut.Decrypt(first).Should().Be(plaintext);
        sut.Decrypt(second).Should().Be(plaintext);
    }

    // ─── Legacy plaintext pass-through ───────────────────────────────────────

    [Theory]
    [InlineData("a legacy plaintext row written before encryption shipped")]
    [InlineData("")]
    [InlineData("enc:v0:not-actually-our-prefix")]
    [InlineData("encrypted-looking but no colon-v1 marker")]
    public void Decrypt_passes_through_any_value_without_the_v1_prefix_unchanged(string legacy)
    {
        var sut = Service();

        sut.Decrypt(legacy).Should().Be(legacy);
    }

    // ─── Tamper detection ────────────────────────────────────────────────────

    [Fact]
    public void Decrypt_throws_when_the_ciphertext_has_been_tampered_with()
    {
        var sut = Service();
        var cipher = sut.Encrypt("authenticated payload")!;

        // Flip the final character of the Base64 body — the GCM auth tag no
        // longer matches, so decryption must fail rather than return garbage.
        var lastChar = cipher[^1];
        var swapped = lastChar == 'A' ? 'B' : 'A';
        var tampered = cipher[..^1] + swapped;

        var act = () => sut.Decrypt(tampered);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_throws_when_the_envelope_payload_is_not_valid_Base64()
    {
        var sut = Service();

        var act = () => sut.Decrypt("enc:v1:not valid base64!!!");

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_throws_when_the_envelope_is_too_short_for_a_nonce_and_tag()
    {
        var sut = Service();
        // Valid Base64, but only a few bytes — far short of nonce(12)+tag(16).
        var tooShort = "enc:v1:" + Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        var act = () => sut.Decrypt(tooShort);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_throws_when_the_ciphertext_was_produced_under_a_different_key()
    {
        var owner = Service();
        // A different 32-byte key.
        var otherKey = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes("a completely different key")));
        var attacker = Service(otherKey);

        var cipher = owner.Encrypt("secret")!;

        var act = () => attacker.Decrypt(cipher);

        act.Should().Throw<CryptographicException>();
    }

    // ─── Local key provider ──────────────────────────────────────────────────

    [Fact]
    public void LocalKeyProvider_reads_the_configured_Base64_dev_key_and_flags_a_warning()
    {
        var provider = new LocalFieldEncryptionKeyProvider(
            Options.Create(new FieldEncryptionOptions { DevKey = DevKeyBase64 }));

        provider.GetKey().Should().HaveCount(32, "AES-256 needs a 32-byte key");
        provider.GetKey().Should().Equal(Convert.FromBase64String(DevKeyBase64));

        var description = provider.Describe(out var isWarning);
        isWarning.Should().BeTrue("the development key must not reach production");
        description.Should().Contain("DEVELOPMENT ONLY");
    }

    [Fact]
    public void LocalKeyProvider_throws_when_no_dev_key_is_configured()
    {
        // Field encryption needs a stable key; there is deliberately no
        // generate-on-startup fallback, so a missing key is a hard error.
        var act = () => new LocalFieldEncryptionKeyProvider(
            Options.Create(new FieldEncryptionOptions { DevKey = null }));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DevKey*");
    }

    [Fact]
    public void LocalKeyProvider_throws_when_the_dev_key_is_not_valid_Base64()
    {
        var act = () => new LocalFieldEncryptionKeyProvider(
            Options.Create(new FieldEncryptionOptions { DevKey = "this is not base64!" }));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LocalKeyProvider_throws_when_the_dev_key_is_not_256_bits()
    {
        // 16 bytes — a valid Base64 string, but AES-128-sized, not AES-256.
        var shortKey = Convert.ToBase64String(new byte[16]);

        var act = () => new LocalFieldEncryptionKeyProvider(
            Options.Create(new FieldEncryptionOptions { DevKey = shortKey }));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*32 bytes*");
    }

    [Fact]
    public void KeyVaultKeyProvider_requires_a_vault_uri()
    {
        // The Key Vault provider is only valid when a vault URI is configured;
        // selection between Local and Key Vault is the registration's job.
        var act = () => new KeyVaultFieldEncryptionKeyProvider(
            Options.Create(new FieldEncryptionOptions { KeyVaultUri = "" }));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*KeyVaultUri*");
    }
}
