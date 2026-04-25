using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using ScholarPath.Application.Common.Exceptions;
namespace ScholarPath.UnitTests.Scholarships
{// في مشروع ScholarPath.UnitTests
    public class GetScholarshipByIdHandlerTests
    {
        private readonly IApplicationDbContext _context;
        private readonly GetScholarshipByIdQueryHandler _handler;

        public GetScholarshipByIdHandlerTests()
        {
            // بنستخدم InMemory عشان نختبر الـ LINQ Queries
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _handler = new GetScholarshipByIdQueryHandler(_context);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenIdDoesNotExist()
        {
            // Arrange
            var query = new GetScholarshipByIdQuery(Guid.NewGuid());

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}







