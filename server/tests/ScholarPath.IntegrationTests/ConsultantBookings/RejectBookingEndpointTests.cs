using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;
using ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.IntegrationTests.ConsultantBookings;

public sealed class RejectBookingEndpointTests : IntegrationTestBase
{
    public RejectBookingEndpointTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Post_RejectBooking_ReturnsNoContent_AndRejectsBooking()
    {
        var studentId = Guid.NewGuid();
        var consultantId = Guid.NewGuid();

        var studentEmail = $"student.{studentId:N}@scholarpath.local";
        var consultantEmail = $"consultant.{consultantId:N}@scholarpath.local";

        var scheduledStartAt = DateTimeOffset.UtcNow.AddDays(2);
        var scheduledEndAt = scheduledStartAt.AddHours(1);

        await ExecuteScopeAsync(async sp =>
        {
            var db = GetDb(sp);

            var existingStudent = await db.Users.FindAsync(studentId);
            if (existingStudent is null)
            {
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
            }

            var existingConsultant = await db.Users.FindAsync(consultantId);
            if (existingConsultant is null)
            {
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
            }

            var consultantProfile = db.UserProfiles.FirstOrDefault(p => p.UserId == consultantId);
            if (consultantProfile is null)
            {
                db.UserProfiles.Add(new UserProfile
                {
                    UserId = consultantId,
                    SessionFeeUsd = 100m,
                    Timezone = "UTC"
                });
            }

            await db.SaveChangesAsync();
        });

        var requestBooking = new RequestBookingCommand(
            ConsultantId: consultantId,
            AvailabilityId: null,
            ScheduledStartAt: scheduledStartAt,
            ScheduledEndAt: scheduledEndAt,
            Timezone: "UTC",
            Notes: "Integration test booking");

        Guid bookingId = Guid.Empty;

        await ExecuteScopeAsync(async sp =>
        {
            var currentUser = GetCurrentUser(sp);
            currentUser.SetUser(studentId, studentEmail, "Student");

            var sender = sp.GetRequiredService<MediatR.ISender>();
            bookingId = await sender.Send(requestBooking);
        });

        bookingId.Should().NotBeEmpty();

        await ExecuteScopeAsync(async sp =>
        {
            var currentUser = GetCurrentUser(sp);
            currentUser.SetUser(consultantId, consultantEmail, "Consultant");
            await Task.CompletedTask;
        });

        var command = new RejectBookingCommand(
            BookingId: bookingId);

        var response = await Client.PostAsJsonAsync(
            $"/api/bookings/{bookingId:D}/reject",
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

            booking!.StudentId.Should().Be(studentId);
            booking.ConsultantId.Should().Be(consultantId);
            booking.Status.Should().Be(BookingStatus.Rejected);
            booking.RejectedAt.Should().NotBeNull();
            booking.StripePaymentIntentId.Should().NotBeNullOrWhiteSpace();

            var payment = db.Payments.FirstOrDefault(p =>
                p.StripePaymentIntentId == booking.StripePaymentIntentId);

            payment.Should().NotBeNull();
            payment!.Type.Should().Be(PaymentType.ConsultantBooking);
            payment.PayerUserId.Should().Be(studentId);
            payment.PayeeUserId.Should().Be(consultantId);
            payment.StripePaymentIntentId.Should().Be(booking.StripePaymentIntentId);
        });
    }
}
