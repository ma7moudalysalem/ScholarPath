using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

namespace ScholarPath.Application.Scholarships.Commands;

public class UpdateScholarshipCommandValidator : AbstractValidator<UpdateScholarshipCommand>
{
    public UpdateScholarshipCommandValidator()
    {
        // 1. التأكد من وجود Id للمنحة
        RuleFor(v => v.Id)
            .NotEmpty();

        // 2. التحقق من العناوين (إنجليزي وعربي)
        RuleFor(v => v.TitleEn)
            .MaximumLength(200)
            .NotEmpty().WithMessage("العنوان بالإنجليزية مطلوب.");

        RuleFor(v => v.TitleAr)
            .MaximumLength(200)
            .NotEmpty().WithMessage("العنوان بالعربية مطلوب.");

        // 3. التحقق من الوصف
        RuleFor(v => v.DescriptionEn)
            .NotEmpty().WithMessage("الوصف بالإنجليزية مطلوب.");

        RuleFor(v => v.DescriptionAr)
            .NotEmpty().WithMessage("الوصف بالعربية مطلوب.");

        // 4. التحقق من التاريخ (يجب أن يكون في المستقبل)
        RuleFor(v => v.Deadline)
            .NotEmpty()
            .GreaterThan(DateTime.UtcNow).WithMessage("تاريخ انتهاء التقديم يجب أن يكون في المستقبل.");

        // 5. التأكد من اختيار قسم (Category)
        RuleFor(v => v.CategoryId)
            .NotEmpty().WithMessage("يجب اختيار قسم للمنحة.");
    }
}
