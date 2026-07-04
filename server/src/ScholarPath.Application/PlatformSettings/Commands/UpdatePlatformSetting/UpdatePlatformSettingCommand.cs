using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using System.Globalization;

namespace ScholarPath.Application.PlatformSettings.Commands.UpdatePlatformSetting;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Updates the value of an existing platform setting (PB-011). The store has a
/// fixed key set — this only updates; there is no "create setting" path.
/// </summary>
[Auditable(AuditAction.ConfigChanged, "PlatformSetting",
    SummaryTemplate = "Changed platform setting {Key}")]
public sealed record UpdatePlatformSettingCommand(string Key, string Value)
    : IRequest<PlatformSettingDto>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class UpdatePlatformSettingCommandValidator
    : AbstractValidator<UpdatePlatformSettingCommand>
{
    public UpdatePlatformSettingCommandValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("A setting key is required.")
            .MaximumLength(200);

        // Value may legitimately be empty (e.g. clearing the announcement banner),
        // so only the length ceiling is enforced here.
        RuleFor(x => x.Value)
            .NotNull().WithMessage("A value is required.")
            .MaximumLength(4000);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class UpdatePlatformSettingCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<UpdatePlatformSettingCommandHandler> logger)
    : IRequestHandler<UpdatePlatformSettingCommand, PlatformSettingDto>
{
    public async Task<PlatformSettingDto> Handle(
        UpdatePlatformSettingCommand request, CancellationToken ct)
    {
        // Platform settings govern site-wide behaviour — administrators only.
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException(
                "Only an administrator can change platform settings.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var setting = await db.PlatformSettings
            .FirstOrDefaultAsync(s => s.Key == request.Key, ct)
            ?? throw new NotFoundException(nameof(PlatformSetting), request.Key);

        ValidateValueForType(setting.ValueType, request.Value);

        var oldValue = setting.Value;

        setting.Value = request.Value;
        setting.UpdatedByAdminId = adminId;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Platform setting {Key} changed by admin {AdminId}.", setting.Key, adminId);

        // DES-01: switching the platform to free mode (payments.enabled true→false)
        // does NOT retroactively waive commission on already-captured paid bookings/
        // reviews — those still carry a ProfitShareAmountCents that will be paid out.
        // A full retroactive reconciliation is a deliberate v2 decision (needs a
        // team call on whether the waiver is intended), so the safe mitigation now
        // is to surface the exposure: log the captured, not-yet-paid-out rows that
        // still carry commission so an admin can review them before the next payout.
        if (setting.Key == PlatformSettingsKeys.PaymentsEnabled &&
            IsTrue(oldValue) && !IsTrue(request.Value))
        {
            await WarnOnCapturedCommissionAsync(db, adminId, logger, ct).ConfigureAwait(false);
        }

        return new PlatformSettingDto(
            setting.Id, setting.Key, setting.Value, setting.ValueType,
            setting.DescriptionEn, setting.DescriptionAr, setting.Category, setting.UpdatedAt);
    }

    private static bool IsTrue(string? value) =>
        bool.TryParse(value, out var b) && b;

    /// <summary>
    /// DES-01 mitigation — logs the captured (or partially-refunded) payments that
    /// have not been paid out yet but still carry a platform commission, so an admin
    /// can decide whether to waive them before the payout job runs. Read-only; never
    /// mutates the ledger (retroactive waiver is a v2 decision).
    /// </summary>
    private static async Task WarnOnCapturedCommissionAsync(
        IApplicationDbContext db, Guid adminId,
        ILogger logger, CancellationToken ct)
    {
        var affected = await db.Payments
            .Where(p =>
                (p.Status == PaymentStatus.Captured || p.Status == PaymentStatus.PartiallyRefunded) &&
                p.PayoutId == null &&
                p.ProfitShareAmountCents > 0)
            .Select(p => new { p.AmountCents, p.ProfitShareAmountCents })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (affected.Count == 0)
        {
            logger.LogInformation(
                "Free-mode toggle by admin {AdminId}: no captured, not-yet-paid-out payments carry commission.",
                adminId);
            return;
        }

        var grossCents = affected.Sum(p => p.AmountCents);
        var commissionCents = affected.Sum(p => p.ProfitShareAmountCents);

        logger.LogWarning(
            "Free-mode toggle by admin {AdminId}: {Count} captured/partially-refunded payment(s) " +
            "still awaiting payout carry a platform commission that free mode will NOT retroactively waive " +
            "(gross ${GrossUsd:0.00}, commission ${CommissionUsd:0.00}). Review before the next payout.",
            adminId, affected.Count, grossCents / 100m, commissionCents / 100m);
    }

    /// <summary>Rejects values that do not match the setting's declared type.</summary>
    private static void ValidateValueForType(PlatformSettingType type, string value)
    {
        switch (type)
        {
            case PlatformSettingType.Boolean
                when value is not ("true" or "false"):
                throw new ConflictException(
                    "This setting expects a boolean value of 'true' or 'false'.");

            case PlatformSettingType.Number
                when !decimal.TryParse(
                    value, NumberStyles.Number, CultureInfo.InvariantCulture, out _):
                throw new ConflictException(
                    "This setting expects a numeric value.");

            // Text accepts anything; valid Boolean/Number fall through.
            default:
                break;
        }
    }
}
