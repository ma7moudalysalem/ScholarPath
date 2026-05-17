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
        if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("SuperAdmin"))
            throw new ForbiddenAccessException(
                "Only an administrator can change platform settings.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var setting = await db.PlatformSettings
            .FirstOrDefaultAsync(s => s.Key == request.Key, ct)
            ?? throw new NotFoundException(nameof(PlatformSetting), request.Key);

        ValidateValueForType(setting.ValueType, request.Value);

        setting.Value = request.Value;
        setting.UpdatedByAdminId = adminId;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Platform setting {Key} changed by admin {AdminId}.", setting.Key, adminId);

        return new PlatformSettingDto(
            setting.Id, setting.Key, setting.Value, setting.ValueType,
            setting.DescriptionEn, setting.DescriptionAr, setting.Category, setting.UpdatedAt);
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
