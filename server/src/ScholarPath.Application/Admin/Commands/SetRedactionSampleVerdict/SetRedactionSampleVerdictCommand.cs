using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.SetRedactionSampleVerdict;

/// <summary>
/// Admin verdict for a single redaction audit sample (PB-017 US-178 / FR-255).
/// Setting <c>Verdict = Clean</c> signals the redaction worked; the other
/// values flag which PII category leaked through the source-generated regex.
/// </summary>
[Auditable(AuditAction.Update, "AiRedactionAuditSample",
    SummaryTemplate = "Redaction verdict set ({Verdict})")]
public sealed record SetRedactionSampleVerdictCommand(
    Guid SampleId,
    RedactionVerdict Verdict) : IRequest<Unit>;
