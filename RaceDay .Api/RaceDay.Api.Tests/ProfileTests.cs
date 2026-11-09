using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class ProfileTests
{
    [Fact]
    public async Task GetProfile_AfterParticipantLogin_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var registerRequest = new
        {
            FirstName = "Ayanda",
            LastName = "ProfileTest",
            Email = "ayanda.profile.test@example.com",
            Password = "TestPassword123!",
            PhoneNumber = "0712345678",
            Role = "Participant"
        };

        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            registerRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

        var loginRequest = new
        {
            Email = "ayanda.profile.test@example.com",
            Password = "TestPassword123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var profileResponse = await client.GetAsync(
            "/api/profile");
        Console.WriteLine(
            $"Profile status: {(int)profileResponse.StatusCode} {profileResponse.StatusCode}");

        Assert.Equal(
            HttpStatusCode.OK,
            profileResponse.StatusCode);

        var profile = await profileResponse.Content
            .ReadFromJsonAsync<ProfileResponse>();

        Assert.NotNull(profile);
        Assert.Equal("Ayanda", profile.FirstName);
        Assert.Equal("ProfileTest", profile.LastName);
        Assert.Equal(
            "ayanda.profile.test@example.com",
            profile.Email);
    }

    private sealed class ProfileResponse
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
    [Fact]
    public async Task GetProfile_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/profile");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task UpdateProfile_AfterParticipantLogin_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var registerRequest = new
        {
            FirstName = "Lerato",
            LastName = "Original",
            Email = "lerato.profile.update@example.com",
            Password = "TestPassword123!",
            PhoneNumber = "0712345678",
            Role = "Participant"
        };

        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            registerRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = "lerato.profile.update@example.com",
                Password = "TestPassword123!"
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/profile",
            new
            {
                FirstName = "Lerato",
                LastName = "Updated",
                Email = "lerato.updated@example.com",
                PhoneNumber = "0723456789"
            });
        Console.WriteLine(
            await updateResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var profileResponse = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profile = await profileResponse.Content
            .ReadFromJsonAsync<ProfileResponse>();

        Assert.NotNull(profile);
        Assert.Equal("Lerato", profile.FirstName);
        Assert.Equal("Updated", profile.LastName);
        Assert.Equal("lerato.updated@example.com", profile.Email);
    }
    [Fact]
    public async Task UpdateProfile_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var updateRequest = new
        {
            FirstName = "Unauthorized",
            LastName = "Update",
            Email = "unauthorized.update@example.com",
            PhoneNumber = "0712345678"
        };

        var response = await client.PutAsJsonAsync(
            "/api/profile",
            updateRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task GetProfile_WithAnotherUsersIdInUrl_DoesNotExposeProfile()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var response = await client.GetAsync("/api/profile/2");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}