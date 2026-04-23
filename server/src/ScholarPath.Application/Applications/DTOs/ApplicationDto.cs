using System;
using System.Collections.Generic;
using System.Text;

namespace ScholarPath.Application.Applications.DTOs
{
    public record ApplicationDto
   (
        Guid Id,
        Guid ScholarshipId,
        string Status,
        DateTimeOffset SubmittedAt,
        string? PersonalNotes);
}
