using System;
using System.Collections.Generic;
using System.Text;


using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ScholarPath.IntegrationTests.Helpers;

/// <summary>
/// Generates JWT tokens for integration test scenarios.
/// Uses a fixed test secret — never use in production.
/// </summary>
public static class TestJwtHelper
{
    private const string TestSecret =
        "TestSecret_ScholarPath_IntegrationTests_32chars!";

    public static string GenerateStudentToken(Guid userId)
        => GenerateToken(userId, "Student");

    public static string GenerateAdminToken(Guid userId)
        => GenerateToken(userId, "Admin");

    private static string GenerateToken(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(TestSecret));

        var token = new JwtSecurityToken(
            issuer: "ScholarPath.Tests",
            audience: "ScholarPath.Tests",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
