using Microsoft.AspNetCore.Mvc.Testing;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace ScholarPath.IntegrationTests.Scholarships
{
    public class ScholarshipApiTests: IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ScholarshipApiTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenScholarshipDoesNotExist()
        {
            // Arrange
            var fakeId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync(new Uri($"/api/v1/scholarships/{fakeId}" ,UriKind.Relative));

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetScholarships_ShouldReturnSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync(new Uri("/api/v1/scholarships?pageNumber=1&pageSize=10" ,UriKind.Relative));

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PaginatedList<ScholarshipDto>>();
            result.Should().NotBeNull();
        }
    }
    
    }

