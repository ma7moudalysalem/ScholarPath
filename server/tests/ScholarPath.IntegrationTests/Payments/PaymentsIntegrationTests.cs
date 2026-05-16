using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.Payments.Commands.CreatePaymentIntent;
using ScholarPath.Application.Payments.Queries.GetPayment;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.IntegrationTests.Payments;

public class PaymentsIntegrationTests : IClassFixture<PaymentsWebApplicationFactory>
{
    private readonly PaymentsWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentsIntegrationTests(PaymentsWebApplicationFactory factory)
    {
        _factory = factory;
        // Student client 
        _client = factory.CreateAuthenticatedClient(
            factory.SeededStudentId, "Student");
    }

    // ── Seed Helpers ──────────────────────────────────────────────────────────

    private async Task SeedProfitShareConfigAsync(PaymentType type)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (db.ProfitShareConfigs.Any(c => c.PaymentType == type && c.EffectiveTo == null))
            return;

        db.ProfitShareConfigs.Add(new ProfitShareConfig
        {
            Id = Guid.NewGuid(),
            PaymentType = type,
            Percentage = 0.10m,
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-1),
            EffectiveTo = null,
            SetByAdminId = _factory.SeededAdminId,
        });

        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedPaymentAsync(
        PaymentStatus status,
        PaymentType type = PaymentType.ConsultantBooking,
        long amountCents = 5000)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = type,
            Status = status,
            AmountCents = amountCents,
            Currency = "USD",
            ProfitShareAmountCents = 500,
            PayeeAmountCents = 4500,
            RefundedAmountCents = 0,
            PayerUserId = _factory.SeededStudentId,
            PayeeUserId = _factory.SeededConsultantId,
            StripePaymentIntentId = $"pi_test_{Guid.NewGuid():N}",
            IdempotencyKey = $"test:{Guid.NewGuid():N}",
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return payment.Id;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/payments/intent
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateIntent_ValidCommand_Returns201AndPaymentRow()
    {
        await SeedProfitShareConfigAsync(PaymentType.ConsultantBooking);
        var bookingId = Guid.NewGuid();

        var command = new CreatePaymentIntentCommand(
            Type: PaymentType.ConsultantBooking,
            AmountCents: 5000,
            Currency: "USD",
            PayeeUserId: _factory.SeededConsultantId,
            RelatedBookingId: bookingId,
            RelatedApplicationId: null);

        var response = await _client.PostAsJsonAsync("/api/payments/intent", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreatePaymentIntentResult>();
        result.Should().NotBeNull();
        result!.PaymentId.Should().NotBeEmpty();
        result.ClientSecret.Should().NotBeNullOrEmpty();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var payment = db.Payments.FirstOrDefault(p => p.Id == result.PaymentId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Pending);
        payment.AmountCents.Should().Be(5000);
        payment.Type.Should().Be(PaymentType.ConsultantBooking);
        payment.ProfitShareAmountCents.Should().Be(500);
        payment.PayeeAmountCents.Should().Be(4500);
    }

    [Fact]
    public async Task CreateIntent_ZeroAmount_Returns400()
    {
        var command = new CreatePaymentIntentCommand(
            Type: PaymentType.ConsultantBooking,
            AmountCents: 0,
            Currency: "USD",
            PayeeUserId: null,
            RelatedBookingId: Guid.NewGuid(),
            RelatedApplicationId: null);

        var response = await _client.PostAsJsonAsync("/api/payments/intent", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateIntent_Idempotent_ReturnsSamePaymentId()
    {
        await SeedProfitShareConfigAsync(PaymentType.ConsultantBooking);
        var bookingId = Guid.NewGuid();

        var command = new CreatePaymentIntentCommand(
            Type: PaymentType.ConsultantBooking,
            AmountCents: 3000,
            Currency: "USD",
            PayeeUserId: _factory.SeededConsultantId,
            RelatedBookingId: bookingId,
            RelatedApplicationId: null);

        var first = await _client.PostAsJsonAsync("/api/payments/intent", command);
        var second = await _client.PostAsJsonAsync("/api/payments/intent", command);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var r1 = await first.Content.ReadFromJsonAsync<CreatePaymentIntentResult>();
        var r2 = await second.Content.ReadFromJsonAsync<CreatePaymentIntentResult>();

        r1!.PaymentId.Should().Be(r2!.PaymentId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/capture
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Capture_HeldPayment_Returns204AndStatusCaptured()
    {
        var paymentId = await SeedPaymentAsync(PaymentStatus.Held);

        var response = await _client.PostAsync(
            new Uri($"/api/payments/{paymentId}/capture", UriKind.Relative),
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var payment = db.Payments.First(p => p.Id == paymentId);

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Capture_AlreadyCaptured_Returns404()
    {
        var paymentId = await SeedPaymentAsync(PaymentStatus.Captured);

        var response = await _client.PostAsync(
            new Uri($"/api/payments/{paymentId}/capture", UriKind.Relative),
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Capture_NonExistentPayment_Returns404()
    {
        var response = await _client.PostAsync(
            new Uri($"/api/payments/{Guid.NewGuid()}/capture", UriKind.Relative),
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/refund
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refund_HeldPayment_FullRefund_Returns204AndStatusRefunded()
    {
        var paymentId = await SeedPaymentAsync(PaymentStatus.Held, amountCents: 5000);

        var body = new { amountCents = (long?)null, reason = "full refund test" };

        var response = await _client.PostAsJsonAsync(
            $"/api/payments/{paymentId}/refund", body);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var payment = db.Payments.First(p => p.Id == paymentId);

        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmountCents.Should().Be(5000);
        payment.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Refund_CapturedPayment_PartialRefund_Returns204AndStatusPartiallyRefunded()
    {
        var paymentId = await SeedPaymentAsync(PaymentStatus.Captured, amountCents: 10000);

        var body = new { amountCents = (long?)3000, reason = "partial refund test" };

        var response = await _client.PostAsJsonAsync(
            $"/api/payments/{paymentId}/refund", body);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var payment = db.Payments.First(p => p.Id == paymentId);

        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmountCents.Should().Be(3000);
    }

    [Fact]
    public async Task Refund_AlreadyRefunded_Returns404()
    {
        var paymentId = await SeedPaymentAsync(PaymentStatus.Refunded);

        var body = new { amountCents = (long?)null, reason = "duplicate" };

        var response = await _client.PostAsJsonAsync(
            $"/api/payments/{paymentId}/refund", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Refund_ZeroAmount_Returns400()
    {
        var paymentId = await SeedPaymentAsync(PaymentStatus.Held);

        var body = new { amountCents = (long?)0, reason = "zero" };

        var response = await _client.PostAsJsonAsync(
            $"/api/payments/{paymentId}/refund", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/payments/{id}
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingPayment_Returns200WithCorrectData()
    {
        var paymentId = await SeedPaymentAsync(PaymentStatus.Held);

        var response = await _client.GetAsync(
            new Uri($"/api/payments/{paymentId}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<PaymentDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(paymentId);
        dto.Status.Should().Be(PaymentStatus.Held);
        dto.AmountCents.Should().Be(5000);
        dto.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetById_NonExistentPayment_Returns404()
    {
        var response = await _client.GetAsync(
            new Uri($"/api/payments/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
