using FluentAssertions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Application.ConsultantBookings.Queries.BrowseConsultants;
using ScholarPath.Application.ConsultantBookings.Queries.GetConsultantAvailability;
using ScholarPath.Application.ConsultantBookings.Queries.GetConsultantById;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// Covers the consultant-marketplace query handlers that delegate to
/// <see cref="IConsultantReadService"/>:
/// <see cref="BrowseConsultantsQuery"/>, <see cref="GetConsultantByIdQuery"/>
/// and <see cref="GetConsultantAvailabilityQuery"/>. The service is mocked —
/// these tests confirm delegation and the not-found translation, mirroring
/// <c>ExportUsersCsvQueryHandlerTests</c>'s mocked-service style.
/// </summary>
public sealed class ConsultantQueryHandlerTests
{
    private readonly IConsultantReadService _service = Substitute.For<IConsultantReadService>();

    // ── BrowseConsultantsQuery ──────────────────────────────────────────────────

    [Fact]
    public async Task Browse_DelegatesToReadServiceAndReturnsItsResult()
    {
        var rows = new[]
        {
            new ConsultantSummaryDto { Id = Guid.NewGuid(), Name = "Sarah Adel" },
        };
        _service.BrowseConsultantsAsync(Arg.Any<CancellationToken>())
            .Returns(rows);

        var handler = new BrowseConsultantsQueryHandler(_service);

        var result = await handler.Handle(new BrowseConsultantsQuery(), CancellationToken.None);

        result.Should().BeSameAs(rows);
        await _service.Received(1).BrowseConsultantsAsync(Arg.Any<CancellationToken>());
    }

    // ── GetConsultantByIdQuery ──────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsDetail_WhenReadServiceFindsTheConsultant()
    {
        var id = Guid.NewGuid();
        var detail = new ConsultantDetailDto { Id = id, Name = "Sarah Adel" };
        _service.GetConsultantDetailAsync(id, Arg.Any<CancellationToken>())
            .Returns(detail);

        var handler = new GetConsultantByIdQueryHandler(_service);

        var result = await handler.Handle(
            new GetConsultantByIdQuery(id), CancellationToken.None);

        result.Should().BeSameAs(detail);
    }

    [Fact]
    public async Task GetById_ThrowsNotFound_WhenReadServiceReturnsNull()
    {
        _service.GetConsultantDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ConsultantDetailDto?)null);

        var handler = new GetConsultantByIdQueryHandler(_service);

        var act = () => handler.Handle(
            new GetConsultantByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetConsultantAvailabilityQuery ──────────────────────────────────────────

    [Fact]
    public async Task GetAvailability_ReturnsSlots_WhenReadServiceFindsTheConsultant()
    {
        var id = Guid.NewGuid();
        var slots = new[]
        {
            new BookableSlotDto
            {
                AvailabilityId = Guid.NewGuid(),
                StartAt = DateTimeOffset.UtcNow.AddDays(1),
                EndAt = DateTimeOffset.UtcNow.AddDays(1).AddMinutes(45),
                DurationMinutes = 45,
            },
        };
        _service.GetConsultantOpenSlotsAsync(id, Arg.Any<CancellationToken>())
            .Returns(slots);

        var handler = new GetConsultantAvailabilityQueryHandler(_service);

        var result = await handler.Handle(
            new GetConsultantAvailabilityQuery(id), CancellationToken.None);

        result.Should().BeSameAs(slots);
    }

    [Fact]
    public async Task GetAvailability_ThrowsNotFound_WhenReadServiceReturnsNull()
    {
        _service.GetConsultantOpenSlotsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<BookableSlotDto>?)null);

        var handler = new GetConsultantAvailabilityQueryHandler(_service);

        var act = () => handler.Handle(
            new GetConsultantAvailabilityQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
