using MediatR;
using ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgrade;
using ScholarPath.Application.UpgradeRequests.DTOs;

namespace ScholarPath.Application.UpgradeRequests.Commands.SubmitCompanyUpgrade;

public record SubmitCompanyUpgradeCommand(
    string CompanyName,
    string Country,
    string? Website,
    string ContactPersonName,
    string ContactEmail,
    string? ContactPhone,
    string CompanyRegistrationNumber) : IRequest<SubmitUpgradeResponse>;
