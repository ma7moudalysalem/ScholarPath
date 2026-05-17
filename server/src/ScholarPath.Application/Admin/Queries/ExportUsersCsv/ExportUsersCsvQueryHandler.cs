using System.Globalization;
using System.Text;
using MediatR;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Queries.ExportUsersCsv;

public sealed class ExportUsersCsvQueryHandler(IAdminReadService admin)
    : IRequestHandler<ExportUsersCsvQuery, CsvFileResult>
{
    // AdminReadService clamps page size to 100; page through to cover the
    // whole result set, with a hard ceiling so a runaway export can't OOM.
    private const int PageSize = 100;
    private const int MaxRows = 50_000;

    public async Task<CsvFileResult> Handle(ExportUsersCsvQuery request, CancellationToken ct)
    {
        var rows = new List<AdminUserRow>();
        var page = 1;

        while (rows.Count < MaxRows)
        {
            var pageResult = await admin.SearchUsersAsync(
                request.Search,
                request.Status,
                request.Role,
                request.IncludeDeleted,
                page,
                PageSize,
                ct);

            if (pageResult.Items.Count == 0)
                break;

            rows.AddRange(pageResult.Items);

            if (rows.Count >= pageResult.Total || pageResult.Items.Count < PageSize)
                break;

            page++;
        }

        var sb = new StringBuilder();
        sb.Append("Id,Email,FullName,AccountStatus,IsOnboardingComplete,Roles,CreatedAt,LastLoginAt,IsAtRisk,RiskScore\r\n");

        foreach (var r in rows)
        {
            sb.Append(Csv(r.Id.ToString())).Append(',')
              .Append(Csv(r.Email)).Append(',')
              .Append(Csv(r.FullName)).Append(',')
              .Append(Csv(r.AccountStatus.ToString())).Append(',')
              .Append(Csv(r.IsOnboardingComplete ? "true" : "false")).Append(',')
              .Append(Csv(string.Join("; ", r.Roles))).Append(',')
              .Append(Csv(r.CreatedAt.ToString("O", CultureInfo.InvariantCulture))).Append(',')
              .Append(Csv(r.LastLoginAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty)).Append(',')
              .Append(Csv(r.IsAtRisk ? "true" : "false")).Append(',')
              .Append(Csv(r.RiskScore?.ToString(CultureInfo.InvariantCulture) ?? string.Empty))
              .Append("\r\n");
        }

        // UTF-8 BOM so Excel reads non-ASCII names (Arabic) correctly.
        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        var fileName = $"scholarpath-users-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
        return new CsvFileResult(fileName, bytes);
    }

    /// <summary>RFC 4180 CSV field escaping — quote when the value contains a comma, quote, or newline.</summary>
    private static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsQuoting = value.Contains(',', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal);

        if (!needsQuoting)
            return value;

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
