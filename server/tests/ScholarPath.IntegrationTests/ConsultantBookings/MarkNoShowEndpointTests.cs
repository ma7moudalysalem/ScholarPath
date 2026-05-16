using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.IntegrationTests.ConsultantBookings;

public sealed class MarkNoShowEndpointTests : IntegrationTestBase
{
    public MarkNoShowEndpointTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Post_MarkNoShow_ByStudent_ReturnsNoContent_AndMarksConsultantNoShow()
    {
        var studentId = Guid.NewGuid();
        var consultantId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var studentEmail = $"student.{studentId:N}@scholarpath.local";
        var consultantEmail = $"consultant.{consultantId:N}@scholarpath.local";
        var paymentIntentId = $"pi_{Guid.NewGuid():N}";

        var scheduledEndAt = DateTimeOffset.UtcNow.AddHours(-1);
        var scheduledStartAt = scheduledEndAt.AddHours(-1);

        await ExecuteScopeAsync(async sp =>
        {
            var db = GetDb(sp);

            db.Users.Add(new ApplicationUser
            {
                Id = studentId,
                UserName = studentEmail,
                NormalizedUserName = studentEmail.ToUpperInvariant(),
                Email = studentEmail,
                NormalizedEmail = studentEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Student",
                AccountStatus = AccountStatus.Active,
                ActiveRole = "Student"
            });

            db.Users.Add(new ApplicationUser
            {
                Id = consultantId,
                UserName = consultantEmail,
                NormalizedUserName = consultantEmail.ToUpperInvariant(),
                Email = consultantEmail,
                NormalizedEmail = consultantEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Consultant",
                AccountStatus = AccountStatus.Active,
                ActiveRole = "Consultant"
            });

            db.UserProfiles.Add(new UserProfile
            {
                UserId = consultantId,
                SessionFeeUsd = 100m,
                Timezone = "UTC"
            });

            db.Payments.Add(new Payment
            {
                Id = paymentId,
                Type = PaymentType.ConsultantBooking,
                Status = PaymentStatus.Captured,
                AmountCents = 10000,
                Currency = "USD",
                ProfitShareAmountCents = 0,
                PayeeAmountCents = 10000,
                RefundedAmountCents = 0,
                PayerUserId = studentId,
                PayeeUserId = consultantId,
                StripePaymentIntentId = paymentIntentId,
                StripeChargeId = $"ch_{Guid.NewGuid():N}",
                IdempotencyKey = $"payment:{bookingId:N}",
                RelatedBookingId = bookingId,
                RelatedApplicationId = null,
                HeldAt = scheduledStartAt.AddHours(-1),
                CapturedAt = scheduledStartAt.AddMinutes(-30),
                RefundedAt = null,
                RefundReason = null,
                FailureReason = null,
                IsDeleted = false,
                DeletedAt = null,
                DeletedByUserId = null
            });

            db.Bookings.Add(new ConsultantBooking
            {
                Id = bookingId,
                StudentId = studentId,
                ConsultantId = consultantId,
                AvailabilityId = null,
                ScheduledStartAt = scheduledStartAt,
                ScheduledEndAt = scheduledEndAt,
                DurationMinutes = 60,
                PriceUsd = 100m,
                Status = BookingStatus.Confirmed,
                RequestedAt = scheduledStartAt.AddHours(-2),
                StripePaymentIntentId = paymentIntentId,
                MeetingUrl = $"https://meet.scholarpath.local/{Guid.NewGuid():N}",
                PaymentId = paymentId,
                ConfirmedAt = scheduledStartAt.AddHours(-1),
                RejectedAt = null,
                ExpiredAt = null,
                CancelledAt = null,
                CompletedAt = null,
                CancellationReason = null,
                CancelledByUserId = null,
                IsNoShowStudent = false,
                IsNoShowConsultant = false,
                NoShowMarkedAt = null,
                IsDeleted = false,
                DeletedAt = null,
                DeletedByUserId = null
            });

            await db.SaveChangesAsync();
        });

        await ExecuteScopeAsync(async sp =>
        {
            var currentUser = GetCurrentUser(sp);
            currentUser.SetUser(studentId, studentEmail, "Student");
            await Task.CompletedTask;
        });

        var command = new MarkNoShowCommand(bookingId);

        var response = await Client.PostAsJsonAsync(
            $"/api/bookings/{bookingId:D}/no-show",
            command);

        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            HttpStatusCode.NoContent,
            $"response body was: {responseBody}");

        await ExecuteScopeAsync(async sp =>
        {
            var db = GetDb(sp);

            var booking = await db.Bookings.FindAsync(bookingId);
            booking.Should().NotBeNull();

            booking!.Status.Should().Be(BookingStatus.NoShowConsultant);
            booking.CancellationReason.Should().Be(CancellationReason.ConsultantNoShow);
            booking.IsNoShowConsultant.Should().BeTrue();
            booking.IsNoShowStudent.Should().BeFalse();
            booking.NoShowMarkedAt.Should().NotBeNull();
            booking.StripePaymentIntentId.Should().Be(paymentIntentId);

            var payment = db.Payments.FirstOrDefault(p => p.Id == paymentId);
            payment.Should().NotBeNull();
            payment!.StripePaymentIntentId.Should().Be(paymentIntentId);
            payment.PayerUserId.Should().Be(studentId);
            payment.PayeeUserId.Should().Be(consultantId);
        });
    }
}
