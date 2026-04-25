using MediatR;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ScholarPath.Application.Scholarships.Queries
{
    public record GetScholarshipsQuery : IRequest<PaginatedList<ScholarshipDto>>
    {
        public string? SearchTerm { get; init; }
        public Guid? CategoryId { get; init; }
        public string? FundingType { get; init; }
        public string? AcademicLevel { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; } // e.g., "deadline", "newest"
    }

    public class GetScholarshipsQueryHandler(IApplicationDbContext db)
        : IRequestHandler<GetScholarshipsQuery, PaginatedList<ScholarshipDto>>
    {
        public async Task<PaginatedList<ScholarshipDto>> Handle(GetScholarshipsQuery request, CancellationToken ct)
        {
            var query = db.Scholarships
                .AsNoTracking()
                .Where(s => s.Status == ScholarshipStatus.Open); 

            
            if (request.CategoryId.HasValue)
                query = query.Where(s => s.CategoryId == request.CategoryId);

            if (!string.IsNullOrEmpty(request.FundingType) && Enum.TryParse<FundingType>(request.FundingType, out var funding))
                query = query.Where(s => s.FundingType == funding);

            //  (Full-Text Search Logic)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                // Sarch in both English and Arabic fields
                query = query.Where(s => s.TitleEn.Contains(term) || s.TitleAr.Contains(term) ||


                                         s.DescriptionEn.Contains(term) || s.DescriptionAr.Contains(term));
            }

            //  (Sorting)
            query = request.SortBy?.ToLower() switch
            {
                "deadline" => query.OrderBy(s => s.Deadline),
                "newest" => query.OrderByDescending(s => s.CreatedAt),
                _ => query.OrderByDescending(s => s.IsFeatured).ThenByDescending(s => s.CreatedAt)
            };

            
            
            var lang = "en"; // Header

            var result = query.Select(s => new ScholarshipDto
            {
                Id = s.Id,
                Title = lang == "ar" ? s.TitleAr : s.TitleEn,
                Description = lang == "ar" ? s.DescriptionAr : s.DescriptionEn,
                CategoryName = lang == "ar" ? s.Category!.NameAr : s.Category!.NameEn,
                Deadline = s.Deadline,
                Status = s.Status.ToString(),
                Slug = s.Slug
            });

            return await PaginatedList<ScholarshipDto>.CreateAsync(result, request.PageNumber, request.PageSize);
        }
    }
    
    }

