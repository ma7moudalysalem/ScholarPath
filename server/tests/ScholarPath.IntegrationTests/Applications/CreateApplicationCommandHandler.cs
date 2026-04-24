using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.Applications.Commands.CreateApplication;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using ScholarPath.IntegrationTests.Applications;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ScholarPath.IntegrationTests.Applications
{
    public class CreateApplicationTests : IClassFixture<ScholarshipApplicationsFactory>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CreateApplicationTests(ScholarshipApplicationsFactory factory)
        {
            _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        }

        [Fact]
        public async Task Should_Create_Application_Successfully()
        {
            // Arrange
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. تجهيز منحة مفتوحة
            var scholarship = new Scholarship
            {
                Id = Guid.NewGuid(),
                TitleEn = "Test",
                TitleAr = "تجربة",
                Status = ScholarshipStatus.Open,
                Deadline = DateTimeOffset.UtcNow.AddDays(10),
                Slug = "test-scholarship"
            };
            db.Scholarships.Add(scholarship);
            await db.SaveChangesAsync();

            var handler = scope.ServiceProvider.GetRequiredService<CreateApplicationCommandHandler>();
            var command = new CreateApplicationCommand(scholarship.Id, "My notes");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            db.Applications.Any(a => a.ScholarshipId == scholarship.Id).Should().BeTrue();
        }

        [Fact]
        public async Task Should_Throw_Conflict_When_Application_Already_Exists()
        {
            // Arrange
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handler = scope.ServiceProvider.GetRequiredService<CreateApplicationCommandHandler>();
            var scholarship = new Scholarship
            {
                Id = Guid.NewGuid(),
                TitleEn = "Test",
                TitleAr = "تجربة",
                Status = ScholarshipStatus.Open,
                Deadline = DateTimeOffset.UtcNow.AddDays(10),
                Slug = "test-scholarship"
            };
            db.Scholarships.Add(scholarship);

            var existingApp = new ApplicationTracker
            {
                Id = Guid.NewGuid(),
                ScholarshipId = scholarship.Id,
                StudentId = Guid.Parse("00000000-0000-0000-0000-000000000000"), 
                Status = ApplicationStatus.Pending
            };
            db.Applications.Add(existingApp);
            await db.SaveChangesAsync();

            var command = new CreateApplicationCommand(scholarship.Id, "Duplicate application attempt");
            await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<ConflictException>();
        }
    }
}

