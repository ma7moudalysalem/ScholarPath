using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.RequestEmailChange;

/// <summary>
/// FR-231 — the authenticated user requests a change to their registered email.
/// Verifies the new address is unique, then emails a confirmation link to the
/// NEW address. The change only takes effect once that link is confirmed.
/// </summary>
[Auditable(AuditAction.Update, "User")]
public sealed record RequestEmailChangeCommand(string NewEmail) : IRequest;
