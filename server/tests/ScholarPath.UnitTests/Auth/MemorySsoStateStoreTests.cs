using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

/// <summary>SEC-06 / GAP-2 — OAuth <c>state</c> nonce store behaviour.</summary>
public class MemorySsoStateStoreTests
{
    private static MemorySsoStateStore NewStore() =>
        new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void Stored_state_is_valid_once_then_rejected_on_replay()
    {
        var store = NewStore();
        store.Store("abc123");

        store.Consume("abc123").Should().BeTrue();   // legitimate callback
        store.Consume("abc123").Should().BeFalse();  // single-use — replay rejected
    }

    [Fact]
    public void Unknown_state_is_rejected()
    {
        NewStore().Consume("never-issued").Should().BeFalse();
    }

    [Fact]
    public void Blank_state_is_rejected()
    {
        var store = NewStore();
        store.Consume("").Should().BeFalse();
        store.Consume("   ").Should().BeFalse();
    }
}
