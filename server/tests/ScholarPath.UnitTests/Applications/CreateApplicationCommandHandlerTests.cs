using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute; // هذا السطر ضروري جداً لحل مشكلة SubstituteQueryable
using NSubstitute;
using ScholarPath.Application.Applications.Commands.CreateApplication;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.UnitTests.Applications;

public class CreateApplicationCommandHandlerTests
{
    private readonly IApplicationDbContext _db = Substitute.For<IApplicationDbContext>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeService _clock = Substitute.For<IDateTimeService>();
    private readonly Guid _userId = Guid.NewGuid();

    public CreateApplicationCommandHandlerTests()
    {
        // إعداد القيم الافتراضية للـ Mocks
        _currentUser.UserId.Returns(_userId);
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateApplication()
    {
        // Arrange
        var scholarshipId = Guid.NewGuid();
        var command = new CreateApplicationCommand(scholarshipId, "Valid Note");

        var scholarship = new Scholarship
        {
            Id = scholarshipId,
            Status = ScholarshipStatus.Open,
            Mode = ListingMode.InApp
        };

        // تحويل القائمة لـ Mock DbSet متوافق مع NSubstitute
        var scholarshipsList = new List<Scholarship> { scholarship };
        var scholarshipsMock = SubstituteQueryable.Build(scholarshipsList.AsQueryable());
        _db.Scholarships.Returns(scholarshipsMock);

        var applicationsList = new List<ApplicationTracker>();
        var applicationsMock = SubstituteQueryable.Build(applicationsList.AsQueryable());
        _db.Applications.Returns(applicationsMock);

        _mapper.Map<ApplicationDto>(Arg.Any<ApplicationTracker>())
            .Returns(new ApplicationDto(Guid.NewGuid(), scholarshipId, "Pending", DateTimeOffset.UtcNow, "Valid Note"));

        var sut = new CreateApplicationCommandHandler(_db, _mapper, _currentUser, _clock);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ApplicationStatus.Pending.ToString());
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenApplicationExists_ShouldThrowConflict()
    {
        // Arrange
        var scholarshipId = Guid.NewGuid();
        var command = new CreateApplicationCommand(scholarshipId, "Duplicate Note");

        var scholarshipsList = new List<Scholarship>
        {
            new() { Id = scholarshipId, Status = ScholarshipStatus.Open }
        };
        var scholarshipsMock = SubstituteQueryable.Build(scholarshipsList.AsQueryable());
        _db.Scholarships.Returns(scholarshipsMock);

        var applicationsList = new List<ApplicationTracker>
        {
            new() { StudentId = _userId, ScholarshipId = scholarshipId, Status = ApplicationStatus.Pending }
        };
        var applicationsMock = SubstituteQueryable.Build(applicationsList.AsQueryable());
        _db.Applications.Returns(applicationsMock);

        var sut = new CreateApplicationCommandHandler(_db, _mapper, _currentUser, _clock);

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("لديك طلب تقديم نشط بالفعل لهذه المنحة.");
    }
}
