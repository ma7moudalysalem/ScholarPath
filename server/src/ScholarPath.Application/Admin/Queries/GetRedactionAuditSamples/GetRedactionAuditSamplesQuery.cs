using MediatR;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;

namespace ScholarPath.Application.Admin.Queries.GetRedactionAuditSamples;

/// <summary>
/// Paged listing of PII-redaction audit samples for the admin review UI.
/// <paramref name="pendingOnly"/> defaults true so reviewers land on the
/// unchecked backlog by default (PB-017 US-178 / FR-255).
/// </summary>
public sealed record GetRedactionAuditSamplesQuery(
    bool PendingOnly = true,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<RedactionAuditSampleRow>>;
