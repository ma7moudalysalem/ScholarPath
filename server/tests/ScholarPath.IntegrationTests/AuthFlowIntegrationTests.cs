using System.Net.Http.Json;
using System.Text.Json;
using ScholarPath.Domain.Enums;

namespace ScholarPath.IntegrationTests;

public class AuthFlowIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthFlowIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_sets_user_as_unassigned_and_not_onboarded()
    {
        var payload = new
        {
            firstName = "Ali",
            lastName = "Maher",
            email = $"register-{Guid.NewGuid():N}@test.local",
            password = "StrongPass1!",
            confirmPassword = "StrongPass1!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.EnsureSuccessStatusCode();
        var json = await ReadJsonAsync(response);

        var user = json.GetProperty("user");
        Assert.Equal((int)UserRole.Unassigned, user.GetProperty("role").GetInt32());
        Assert.Equal((int)AccountStatus.Active, user.GetProperty("accountStatus").GetInt32());
        Assert.False(user.GetProperty("isOnboardingComplete").GetBoolean());
    }

    [Fact]
    public async Task Refresh_endpoint_rotates_tokens_for_registered_user()
    {
        var register = await RegisterAsync();

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = register.RefreshToken
        });

        refreshResponse.EnsureSuccessStatusCode();
        var refreshBody = await ReadJsonAsync(refreshResponse);

        var newAccessToken = refreshBody.GetProperty("accessToken").GetString();
        var newRefreshToken = refreshBody.GetProperty("refreshToken").GetString();

        Assert.False(string.IsNullOrWhiteSpace(newAccessToken));
        Assert.False(string.IsNullOrWhiteSpace(newRefreshToken));
        Assert.NotEqual(register.RefreshToken, newRefreshToken);
    }

    [Fact]
    public async Task Refresh_endpoint_returns_forbidden_for_inactive_or_suspended_accounts()
    {
        var register = await RegisterAsync();
        await _factory.UpdateUserAccountStateAsync(register.Email, AccountStatus.Suspended, isActive: false);

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = register.RefreshToken
        });

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Onboarding_as_student_activates_account_immediately()
    {
        using var client = _factory.CreateClient();
        var register = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", register.AccessToken);

        var onboardingResponse = await client.PostAsJsonAsync("/api/v1/auth/onboarding", new
        {
            selectedRole = (int)UserRole.Student
        });

        onboardingResponse.EnsureSuccessStatusCode();
        var json = await ReadJsonAsync(onboardingResponse);

        Assert.Equal((int)UserRole.Student, json.GetProperty("role").GetInt32());
        Assert.Equal((int)AccountStatus.Active, json.GetProperty("accountStatus").GetInt32());
        Assert.True(json.GetProperty("isOnboardingComplete").GetBoolean());
    }

    [Fact]
    public async Task Onboarding_as_consultant_sets_account_to_pending()
    {
        using var client = _factory.CreateClient();
        var register = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", register.AccessToken);

        var onboardingResponse = await client.PostAsJsonAsync("/api/v1/auth/onboarding", new
        {
            selectedRole = (int)UserRole.Consultant,
            expertiseArea = "Scholarship guidance",
            bio = "Ten years of experience in student mentoring"
        });

        onboardingResponse.EnsureSuccessStatusCode();
        var json = await ReadJsonAsync(onboardingResponse);

        Assert.Equal((int)UserRole.Unassigned, json.GetProperty("role").GetInt32());
        Assert.Equal((int)AccountStatus.Pending, json.GetProperty("accountStatus").GetInt32());
        Assert.True(json.GetProperty("isOnboardingComplete").GetBoolean());
    }

    [Fact]
    public async Task Admin_approve_upgrade_request_activates_user_role()
    {
        using var client = _factory.CreateClient();

        const string adminEmail = "admin@test.local";
        const string adminPassword = "AdminPass1!";
        await _factory.SeedAdminAsync(adminEmail, adminPassword);

        var register = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", register.AccessToken);

        await client.PostAsJsonAsync("/api/v1/auth/onboarding", new
        {
            selectedRole = (int)UserRole.Consultant,
            expertiseArea = "Admissions",
            bio = "Mentor"
        });

        
        client.DefaultRequestHeaders.Authorization = null;
        var adminLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = adminEmail,
            password = adminPassword
        });
        adminLogin.EnsureSuccessStatusCode();
        var adminJson = await ReadJsonAsync(adminLogin);
        var adminToken = adminJson.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var listResponse = await client.GetAsync("/api/v1/admin/upgrade-requests?status=0");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await ReadJsonAsync(listResponse);
        var requestId = listJson.EnumerateArray().First().GetProperty("id").GetString()!;

        var approveResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/upgrade-requests/{requestId}/approve", new { });
        approveResponse.EnsureSuccessStatusCode();
        var approveJson = await ReadJsonAsync(approveResponse);
        Assert.Equal((int)UpgradeRequestStatus.Approved, approveJson.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Admin_reject_without_notes_returns_bad_request()
    {
        using var client = _factory.CreateClient();

        const string adminEmail = "admin-reject@test.local";
        const string adminPassword = "AdminPass1!";
        await _factory.SeedAdminAsync(adminEmail, adminPassword);

        var register = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", register.AccessToken);

        await client.PostAsJsonAsync("/api/v1/auth/onboarding", new
        {
            selectedRole = (int)UserRole.Company,
            companyName = "Acme Corp"
        });

       
        client.DefaultRequestHeaders.Authorization = null;
        var adminLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = adminEmail,
            password = adminPassword
        });
        adminLogin.EnsureSuccessStatusCode();
        var adminJson = await ReadJsonAsync(adminLogin);
        var adminToken = adminJson.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var listResponse = await client.GetAsync("/api/v1/admin/upgrade-requests?status=0");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await ReadJsonAsync(listResponse);
        var requestId = listJson.EnumerateArray().First().GetProperty("id").GetString()!;

        // Reject with empty reasons should return 400
        var rejectResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/upgrade-requests/{requestId}/reject", new
            {
                reasons = new List<object>(),
                reviewNotes = ""
            });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, rejectResponse.StatusCode);
    }

    private Task<(string AccessToken, string RefreshToken, string Email)> RegisterAsync()
        => RegisterAsync(_client);

    private static async Task<(string AccessToken, string RefreshToken, string Email)> RegisterAsync(HttpClient client)
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email,
            password = "StrongPass1!",
            confirmPassword = "StrongPass1!"
        });

        response.EnsureSuccessStatusCode();
        var json = await ReadJsonAsync(response);
        return (
            json.GetProperty("accessToken").GetString()!,
            json.GetProperty("refreshToken").GetString()!,
            email);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }
}
