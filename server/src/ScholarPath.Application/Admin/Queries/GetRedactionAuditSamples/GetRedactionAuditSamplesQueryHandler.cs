using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Queries.GetRedactionAuditSamples;

public sealed class GetRedactionAuditSamplesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRedactionAuditSamplesQuery, PagedResult<RedactionAuditSampleRow>>
{
    public async Task<PagedResult<RedactionAuditSampleRow>> Handle(
        GetRedactionAuditSamplesQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.AiRedactionAuditSamples.AsNoTracking().AsQueryable();
        if (request.PendingOnly)
        {
            query = query.Where(s => s.Verdict == null);
        }

        var total = await query.CountAsync(ct).ConfigureAwait(false);

        // Join to Users for the reviewer's context — emails are easier to scan
        // than user ids in the admin grid.
        var items = await query
            .OrderByDescending(s => s.SampledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new RedactionAuditSampleRow(
                s.Id,
                s.AiInteractionId,
                s.UserId,
                db.Users.Where(u => u.Id == s.UserId).Select(u => u.Email).FirstOrDefault(),
                s.RedactedPrompt,
                s.SampledAt,
                s.Verdict,
                s.ReviewerUserId,
                s.ReviewedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<RedactionAuditSampleRow>(items, page, pageSize, total);
    }
}
