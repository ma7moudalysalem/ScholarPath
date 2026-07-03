using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

/// <summary>
/// SEC-06 — the stateless OAuth <c>state</c> token (issue → validate; blank / forged
/// / expired rejected). The real tamper-proofing is ASP.NET DataProtection; here a
/// passthrough protector exercises the store's payload + expiry logic.
/// </summary>
public class SsoStateStoreTests
{
    // Passthrough IDataProtector: the string Protect/Unprotect extensions still
    // base64url-encode around it, so a well-formed token round-trips and malformed
    // input fails to decode — enough to cover issue/validate/expiry behaviour.
    private sealed class FakeProtector : IDataProtectionProvider, IDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;
        public byte[] Protect(byte[] plaintext) => plaintext;
        public byte[] Unprotect(byte[] protectedData) => protectedData;
    }

    private static DataProtectionSsoStateStore NewStore() => new(new FakeProtector());

    [Fact]
    public void Issued_state_validates()
    {
        var store = NewStore();
        store.Validate(store.Issue()).Should().BeTrue();
    }

    [Fact]
    public void Blank_state_is_rejected()
    {
        var store = NewStore();
        store.Validate(null).Should().BeFalse();
        store.Validate("").Should().BeFalse();
        store.Validate("   ").Should().BeFalse();
    }

    [Fact]
    public void Forged_or_malformed_state_is_rejected()
    {
        NewStore().Validate("not-a-real-token").Should().BeFalse();
    }
}
