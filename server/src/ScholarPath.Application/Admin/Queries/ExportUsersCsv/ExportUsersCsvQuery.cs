using MediatR;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Queries.ExportUsersCsv;

/// <summary>
/// FR-162 — exports the admin user list as CSV. Honours the same search /
/// status / role filters as the on-screen user table so an admin can export
/// exactly what they are looking at.
/// </summary>
public sealed record ExportUsersCsvQuery(
    string? Search,
    AccountStatus? Status,
    string? Role,
    bool IncludeDeleted) : IRequest<CsvFileResult>;

/// <summary>An in-memory CSV payload plus the filename the controller should serve it as.</summary>
public sealed record CsvFileResult(string FileName, byte[] Content);
