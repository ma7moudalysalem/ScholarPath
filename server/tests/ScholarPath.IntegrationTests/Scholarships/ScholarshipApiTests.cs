using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Testcontainers.MsSql;
using Microsoft.Extensions.Configuration;

namespace ScholarPath.IntegrationTests.Scholarships
{
    public class ScholarshipApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

        public async Task InitializeAsync() => await _dbContainer.StartAsync();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString()
                });
            });
        }

        public override async ValueTask DisposeAsync()
        {
            if (_dbContainer != null)
            {
                await _dbContainer.DisposeAsync();
            }
                await base.DisposeAsync();
            

        }
        Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
        
    }
    public class ScholarshipTests(ScholarshipApiFactory factory)
    : IClassFixture<ScholarshipApiFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task GetById_ShouldReturnArabicDetails_WhenLanguageIsAr()
        {
            // Arr
            var scholarshipId = Guid.Parse("00000000-0000-0000-0000-000000000000"); // Id Scholarship 

            // Act
            var response = await _client.GetAsync(new Uri($"/api/v1/scholarships/{scholarshipId}?language=ar"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ScholarshipDetailDto>();
            result!.Title.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetById_ShouldReturn409Conflict_WhenScholarshipIsClosed()
        {
                // Arrange
                // Closed 
                var closedScholarshipId = Guid.Parse("0000000-0000-0000-0000-000000000000");

                // Act
                var response = await _client.GetAsync(new Uri($"/api/v1/scholarships/{closedScholarshipId}"));

                // Assert
                // Conflict 
                response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            }
           
        }
    }




