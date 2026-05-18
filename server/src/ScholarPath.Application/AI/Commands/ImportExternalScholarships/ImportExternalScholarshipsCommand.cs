using MediatR;
using ScholarPath.Application.Ai.DTOs;

namespace ScholarPath.Application.Ai.Commands.ImportExternalScholarships;

/// <summary>
/// Admin command — imports the bundled <c>external-scholarships</c> dataset as
/// Open scholarship listings. Idempotent: re-running updates the existing
/// imported rows (matched by slug) rather than duplicating them.
/// </summary>
public sealed record ImportExternalScholarshipsCommand : IRequest<DatasetImportResultDto>;
