using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

namespace ScholarPath.Application.Scholarships.Commands;

public class ArchiveScholarshipCommandValidator : AbstractValidator<ArchiveScholarshipCommand>
{
    public ArchiveScholarshipCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("معرف المنحة مطلوب لتنفيذ عملية الأرشفة.");
    }
}
