using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Services;

public interface ICompanyReviewPricingService
{
    Task<(decimal TotalFeeUsd, decimal PlatformFeeUsd, decimal CompanyPayoutUsd)> CalculateFeesAsync(decimal baseReviewFeeUsd, CancellationToken ct);
}

public sealed class CompanyReviewPricingService(IApplicationDbContext db) : ICompanyReviewPricingService
{
    public async Task<(decimal TotalFeeUsd, decimal PlatformFeeUsd, decimal CompanyPayoutUsd)> CalculateFeesAsync(decimal baseReviewFeeUsd, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var profitShareConfig = await db.ProfitShareConfigs
            .AsNoTracking()
            .Where(c => c.PaymentType == PaymentType.CompanyReview && c.EffectiveFrom <= now && (c.EffectiveTo == null || c.EffectiveTo > now))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        decimal platformPercentage = profitShareConfig?.Percentage ?? 0.10m; // Default 10% if not configured

        var platformFeeUsd = Math.Round(baseReviewFeeUsd * platformPercentage, 2);
        var companyPayoutUsd = baseReviewFeeUsd - platformFeeUsd;

        return (baseReviewFeeUsd, platformFeeUsd, companyPayoutUsd);
    }
}
