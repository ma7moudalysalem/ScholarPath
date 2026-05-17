using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.ConfirmEmailChange;

/// <summary>
/// FR-231 — applies a pending email change. The token + new email come from the
/// confirmation link sent to the new address; the user id comes from the
/// authenticated session, so the link must be opened while signed in.
/// </summary>
[Auditable(AuditAction.Update, "User")]
public sealed record ConfirmEmailChangeCommand(string NewEmail, string Token) : IRequest;
