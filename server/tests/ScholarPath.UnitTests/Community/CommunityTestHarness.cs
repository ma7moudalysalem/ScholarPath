using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.UnitTests.Community;

/// <summary>
/// Shared in-memory test harness for the Community module. Spins up an
/// EF in-memory <see cref="ApplicationDbContext"/>, an
/// <see cref="ICurrentUserService"/> mock and a tiny set of seeded users and
/// posts so each test only has to express the differences it cares about.
/// </summary>
internal sealed class CommunityTestHarness : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    public ApplicationDbContext Db { get; }
    public ICurrentUserService CurrentUser { get; } = Substitute.For<ICurrentUserService>();

    public ApplicationUser StudentA { get; } = NewUser("Student", "Student A");
    public ApplicationUser StudentB { get; } = NewUser("Student", "Student B");
    public ApplicationUser Consultant { get; } = NewUser("Consultant", "Consultant");
    public ApplicationUser Company { get; } = NewUser("Company", "Company");
    public ApplicationUser Admin { get; } = NewUser("Admin", "Admin");

    public ForumCategory Category { get; } = new()
    {
        NameEn = "General",
        NameAr = "عام",
        Slug = "general",
        IsActive = true,
    };

    public CommunityTestHarness()
    {
        // Each harness gets a uniquely-named EF InMemory database. Tests can
        // either reuse the shared `Db` instance (for simple read-back) or
        // call `NewContext()` to spin up an independent context sharing the
        // same store — that's the pattern that avoids the InMemory provider's
        // "load-modify-add-child-save" tracking quirk.
        var dbName = Guid.NewGuid().ToString();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        Db = new ApplicationDbContext(_options);
        Db.Users.AddRange(StudentA, StudentB, Consultant, Company, Admin);
        Db.ForumCategories.Add(Category);
        Db.SaveChanges();
        Db.ChangeTracker.Clear();
    }

    /// <summary>
    /// Creates a fresh <see cref="ApplicationDbContext"/> over the same
    /// underlying in-memory store. Use this when a handler needs its own
    /// change-tracking context independent of what seeded data the harness
    /// already wrote — required for the FlagPost flow where the handler
    /// loads-modifies-and-adds-a-child in a single SaveChanges.
    /// </summary>
    public ApplicationDbContext NewContext() => new(_options);

    public void AsStudent(ApplicationUser u)
    {
        ResetRoleStubs();
        CurrentUser.UserId.Returns(u.Id);
        CurrentUser.IsInRole("Student").Returns(true);
        CurrentUser.Roles.Returns(new[] { "Student" });
    }

    public void AsConsultant()
    {
        ResetRoleStubs();
        CurrentUser.UserId.Returns(Consultant.Id);
        CurrentUser.IsInRole("Consultant").Returns(true);
        CurrentUser.Roles.Returns(new[] { "Consultant" });
    }

    public void AsCompany()
    {
        ResetRoleStubs();
        CurrentUser.UserId.Returns(Company.Id);
        CurrentUser.IsInRole("Company").Returns(true);
        CurrentUser.Roles.Returns(new[] { "Company" });
    }

    public void AsAdmin()
    {
        ResetRoleStubs();
        CurrentUser.UserId.Returns(Admin.Id);
        CurrentUser.IsInRole("Admin").Returns(true);
        CurrentUser.Roles.Returns(new[] { "Admin" });
    }

    private void ResetRoleStubs()
    {
        // Re-stub the role check chain so a switch (e.g. AsConsultant after
        // AsStudent) doesn't leave a previous "true" answer hanging.
        CurrentUser.IsInRole(Arg.Any<string>()).Returns(false);
    }

    public async Task<ForumPost> SeedPostAsync(ApplicationUser author, string body = "hello world")
    {
        var post = new ForumPost
        {
            AuthorId = author.Id,
            CategoryId = Category.Id,
            Title = "Test post",
            BodyMarkdown = body,
            ModerationStatus = PostModerationStatus.Visible,
        };
        Db.ForumPosts.Add(post);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return post;
    }

    public async Task<ForumPost> SeedReplyAsync(ApplicationUser author, ForumPost parent, string body = "reply body")
    {
        var parentEntry = Db.Entry(parent);
        if (parentEntry.State == EntityState.Detached)
        {
            Db.Attach(parent);
        }
        var reply = new ForumPost
        {
            AuthorId = author.Id,
            ParentPostId = parent.Id,
            BodyMarkdown = body,
            ModerationStatus = PostModerationStatus.Visible,
        };
        parent.ReplyCount++;
        Db.ForumPosts.Add(reply);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return reply;
    }

    private static ApplicationUser NewUser(string role, string fullName)
    {
        // Lowercase email — by convention, not a culture-neutral normalisation;
        // suppress CA1308 to keep the test fixture realistic.
#pragma warning disable CA1308
        var local = fullName.Replace(' ', '_').ToLowerInvariant();
#pragma warning restore CA1308
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{local}@test.local",
            Email = $"{local}@test.local",
            FirstName = fullName.Split(' ').FirstOrDefault() ?? fullName,
            LastName = fullName.Contains(' ', StringComparison.Ordinal) ? fullName[(fullName.IndexOf(' ', StringComparison.Ordinal) + 1)..] : string.Empty,
            ActiveRole = role,
            AccountStatus = AccountStatus.Active,
        };
    }

    public void Dispose() => Db.Dispose();
}
