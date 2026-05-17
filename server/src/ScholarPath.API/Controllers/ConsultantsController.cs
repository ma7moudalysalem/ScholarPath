using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Application.ConsultantBookings.Queries.BrowseConsultants;
using ScholarPath.Application.ConsultantBookings.Queries.GetConsultantAvailability;
using ScholarPath.Application.ConsultantBookings.Queries.GetConsultantById;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Public consultant-marketplace read endpoints (PB-006): browse consultants,
/// view one consultant's profile, and view a consultant's upcoming open slots.
/// All actions are anonymous-accessible — students browse before they sign in.
/// </summary>
[ApiController]
[Route("api/consultants")]
[AllowAnonymous]
public sealed class ConsultantsController : ControllerBase
{
    private readonly ISender _sender;

    public ConsultantsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lists every active consultant with a profile summary (name, photo,
    /// expertise, rating, session fee, availability summary).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ConsultantSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConsultantSummaryDto>>> Browse(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new BrowseConsultantsQuery(), cancellationToken));

    /// <summary>
    /// Returns one consultant's full public profile detail.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ConsultantDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultantDetailDto>> GetById(
        Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetConsultantByIdQuery(id), cancellationToken));

    /// <summary>
    /// Returns the consultant's upcoming bookable slots — recurring + ad-hoc
    /// availability expanded into concrete windows, excluding times already
    /// taken by a non-cancelled booking.
    /// </summary>
    [HttpGet("{id:guid}/availability")]
    [ProducesResponseType(typeof(IReadOnlyList<BookableSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BookableSlotDto>>> GetAvailability(
        Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetConsultantAvailabilityQuery(id), cancellationToken));
}
