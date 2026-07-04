using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ProfitShare.Commands.SetProfitShareConfig;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Sets the active profit-share rate for a payment type (PB-014). History-preserving:
/// closes the current active config row and opens a new one effective now.
/// </summary>
[Auditable(AuditAction.ConfigChanged, "ProfitShareConfig",
    SummaryTemplate = "Set profit-share rate for {PaymentType} to {Percentage}")]
public sealed record SetProfitShareConfigCommand(
    PaymentType PaymentType,
    decimal Percentage,
    string? Notes = null) : IRequest<ProfitShareConfigDto>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class SetProfitShareConfigCommandValidator
    : AbstractValidator<SetProfitShareConfigCommand>
{
    public SetProfitShareConfigCommandValidator()
    {
        RuleFor(x => x.PaymentType)
            .IsInEnum()
            .WithMessage("Unknown payment type.");

        RuleFor(x => x.Percentage)
            .InclusiveBetween(0m, 0.50m)
            .WithMessage("Profit-share percentage must be between 0 and 0.50 (50%).");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class SetProfitShareConfigCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<SetProfitShareConfigCommandHandler> logger)
    : IRequestHandler<SetProfitShareConfigCommand, ProfitShareConfigDto>
{
    public async Task<ProfitShareConfigDto> Handle(
        SetProfitShareConfigCommand request, CancellationToken ct)
    {
        // Profit-share config governs how money is split — administrators only.
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException(
                "Only an administrator can change the profit-share configuration.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var now = DateTimeOffset.UtcNow;

        var current = await db.ProfitShareConfigs
            .Where(c => c.PaymentType == request.PaymentType && c.EffectiveTo == null)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        // No-op when the rate is unchanged — keep the history clean.
        if (current is not null && current.Percentage == request.Percentage)
        {
            logger.LogInformation(
                "Profit-share for {Type} already {Pct}; no new config row created.",
                request.PaymentType, request.Percentage);
            return ToDto(current);
        }

        if (current is not null)
        {
            current.EffectiveTo = now;
            // Commit the close before inserting the new row: the filtered unique
            // index allows only one EffectiveTo IS NULL row per payment type.
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var next = new ProfitShareConfig
        {
            Id = Guid.NewGuid(),
            PaymentType = request.PaymentType,
            Percentage = request.Percentage,
            EffectiveFrom = now,
            EffectiveTo = null,
            SetByAdminId = adminId,
            Notes = request.Notes,
        };
        db.ProfitShareConfigs.Add(next);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Profit-share for {Type} set to {Pct} by admin {AdminId}.",
            request.PaymentType, request.Percentage, adminId);

        return ToDto(next);
    }

    private static ProfitShareConfigDto ToDto(ProfitShareConfig c) => new(
        c.Id, c.PaymentType, c.Percentage, c.EffectiveFrom, c.EffectiveTo,
        c.SetByAdminId, c.Notes, c.EffectiveTo == null);
}
