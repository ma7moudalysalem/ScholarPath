using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Auth.Validators;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.UnitTests;

public class AuthOnboardingRulesTests
{
    [Fact]
    public void New_user_default_role_is_unassigned()
    {
        var user = new ApplicationUser();

        Assert.Equal(UserRole.Unassigned, user.Role);
        Assert.False(user.IsOnboardingComplete);
    }

    [Theory]
    [InlineData(UserRole.Student, true)]
    [InlineData(UserRole.Consultant, true)]
    [InlineData(UserRole.Company, true)]
    [InlineData(UserRole.Unassigned, false)]
    [InlineData(UserRole.Admin, false)]
    public void Onboarding_validator_allows_only_student_consultant_company(UserRole selectedRole, bool expectedValid)
    {
        var validator = new CompleteOnboardingRequestValidator();
        var request = new CompleteOnboardingRequest(
            selectedRole,
            CompanyName: "Acme",
            ExpertiseArea: "Admissions",
            Bio: "Experienced mentor");

        var result = validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }
}
