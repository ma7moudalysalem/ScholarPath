using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.UnitTests.Scholarships
{
    public class GetScholarshipsQueryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GetScholarshipsQueryHandler _handler;

        public GetScholarshipsQueryTests()
        {
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _handler = new GetScholarshipsQueryHandler(_context);
        }

        [Fact]
        public async Task Handle_ShouldReturnOnlyOpenScholarships()
        {
            // Arrange
            _context.Scholarships.AddRange(
                new Scholarship { TitleEn = "Open 1",TitleAr ="منحة مفتوحة 1" ,DescriptionEn = "Description for open scholarship",DescriptionAr ="وصف المنحة ", Status = ScholarshipStatus.Open, Slug = "s1" },
                new Scholarship { TitleEn = "Draft 1",TitleAr = "غير مرئية 1" ,DescriptionEn = "Description 2" , DescriptionAr ="وصف 2",Status = ScholarshipStatus.Draft, Slug = "s2" }
            );
            await _context.SaveChangesAsync();

            var query = new GetScholarshipsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Title.Should().Be("Open 1");
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldFilterResults()
        {
            // Arrange
            _context.Scholarships.AddRange(
                new Scholarship { TitleEn = "Medical Grant",TitleAr ="منحه طبية", Status = ScholarshipStatus.Open, Slug = "m1", DescriptionEn = "desc",DescriptionAr ="وصف المنحة الطبية " },
                new Scholarship { TitleEn = "Engineering Grant",TitleAr ="وصف المنحة الهندسية " ,Status = ScholarshipStatus.Open, Slug = "e1", DescriptionEn = "desc" ,DescriptionAr ="وصف المنحة الهندسية"}
            );
            await _context.SaveChangesAsync();

            var query = new GetScholarshipsQuery { SearchTerm = "Medical" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().ContainSingle();
            result.Items.First().Title.Should().Be("Medical Grant");
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
        }
    }
}




