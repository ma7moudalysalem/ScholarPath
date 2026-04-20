using ScholarPath.Application.Profile.Queries.GetProfile;
using ScholarPath.Application.Profile.Commands.UpdateProfile;
using ScholarPath.Application.Profile.DTOs;

namespace ScholarPath.UnitTests;

public class ProfileQueryTests
{
    [Fact]
    public void GetProfileQuery_is_valid_record()
    {
        var query = new GetProfileQuery();

        Assert.NotNull(query);
    }

    [Fact]
    public void UpdateProfileCommand_stores_all_fields()
    {
        var command = new UpdateProfileCommand(
            "Ahmed",
            "Salem",
            "Computer Science",
            3.8m,
            "AI, ML",
            "Egypt",
            "Germany",
            "Student interested in AI",
            new DateTime(2000, 1, 15));

        Assert.Equal("Ahmed", command.FirstName);
        Assert.Equal("Salem", command.LastName);
        Assert.Equal("Computer Science", command.FieldOfStudy);
        Assert.Equal(3.8m, command.GPA);
        Assert.Equal("AI, ML", command.Interests);
        Assert.Equal("Egypt", command.Country);
        Assert.Equal("Germany", command.TargetCountry);
        Assert.Equal("Student interested in AI", command.Bio);
        Assert.Equal(new DateTime(2000, 1, 15), command.DateOfBirth);
    }

    [Fact]
    public void UpdateProfileCommand_allows_null_fields()
    {
        var command = new UpdateProfileCommand(
            null, null, null, null, null, null, null, null, null);

        Assert.Null(command.FirstName);
        Assert.Null(command.GPA);
        Assert.Null(command.Country);
    }

    [Fact]
    public void UserProfileDto_stores_completeness()
    {
        var dto = new UserProfileDto(
            Guid.NewGuid(),
            "Madiha",
            "Mostafa",
            "madiha@test.com",
            null,
            "CS",
            3.5m,
            null,
            "Egypt",
            "UK",
            "Bio here",
            null,
            67);

        Assert.Equal("Madiha", dto.FirstName);
        Assert.Equal(67, dto.ProfileCompleteness);
    }
}
