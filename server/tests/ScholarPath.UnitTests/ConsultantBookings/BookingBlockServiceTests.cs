using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;
using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// PB-006R student booking-access block (FR-CBR-21..24): the pure
/// <see cref="BookingBlockService"/> logic plus the RequestBooking guard.
/// </summary>
public sealed class BookingBlockServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Default_profile_is_not_blocked()
    {
        var p = new UserProfile { UserId = Guid.NewGuid() };
        BookingBlockService.IsCurrentlyBlocked(p, Now).Should().BeFalse();
    }

    [Fact]
    public void ApplyBlock_sets_status_reason_and_expiry()
    {
        var p = new UserProfile { UserId = Guid.NewGuid() };

        BookingBlockService.ApplyBlock(p, BookingBlockReason.ValidatedNoShow, 7, Now);

        p.BookingAccessStatus.Should().Be(BookingAccessStatus.BookingBlocked);
        p.BookingBlockReason.Should().Be(BookingBlockReason.ValidatedNoShow);
        p.BookingBlockUntil.Should().Be(Now.AddDays(7));
        BookingBlockService.IsCurrentlyBlocked(p, Now).Should().BeTrue();
    }

    [Fact]
    public void Block_reads_as_expired_once_BlockUntil_passes()
    {
        var p = new UserProfile { UserId = Guid.NewGuid() };
        BookingBlockService.ApplyBlock(p, BookingBlockReason.CancelledLessThan24Hours, 3, Now);

        BookingBlockService.IsCurrentlyBlocked(p, Now.AddDays(3).AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void ApplyBlock_extends_never_shortens()
    {
        var p = new UserProfile { UserId = Guid.NewGuid() };
        BookingBlockService.ApplyBlock(p, BookingBlockReason.ValidatedNoShow, 7, Now); // until +7d

        // A shorter later penalty must not reduce the outstanding block.
        BookingBlockService.ApplyBlock(p, BookingBlockReason.CancelledLessThan24Hours, 3, Now);
        p.BookingBlockUntil.Should().Be(Now.AddDays(7));
        p.BookingBlockReason.Should().Be(BookingBlockReason.ValidatedNoShow);

        // A longer later penalty extends it.
        BookingBlockService.ApplyBlock(p, BookingBlockReason.FalseNoShowReport, 14, Now);
        p.BookingBlockUntil.Should().Be(Now.AddDays(14));
        p.BookingBlockReason.Should().Be(BookingBlockReason.FalseNoShowReport);
    }

    [Fact]
    public async Task RequestBooking_is_rejected_for_a_blocked_student_without_touching_Stripe()
    {
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var studentId = Guid.NewGuid();

        db.UserProfiles.Add(new UserProfile
        {
            UserId = studentId,
            BookingAccessStatus = BookingAccessStatus.BookingBlocked,
            BookingBlockReason = BookingBlockReason.ValidatedNoShow,
            BookingBlockUntil = DateTimeOffset.UtcNow.AddDays(5),
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.IsInRole("Student").Returns(true);
        currentUser.UserId.Returns(studentId);
        var stripe = Substitute.For<IStripeService>();
        var publisher = Substitute.For<IPublisher>();
        // The consultant is eligible; this test exercises the student-block guard.
        var eligibility = Substitute.For<IConsultantEligibilityService>();
        eligibility.CanActAsConsultantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new RequestBookingCommandHandler(db, currentUser, stripe, publisher, eligibility);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var command = new RequestBookingCommand(
            Guid.NewGuid(), null, start, start.AddMinutes(45), "UTC", null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BookingDomainException>()
            .WithMessage("*blocked*");
        await stripe.DidNotReceiveWithAnyArgs().CreatePaymentIntentAsync(
            default, default!, default!, default!, default!, default);

        db.Dispose();
    }
}
