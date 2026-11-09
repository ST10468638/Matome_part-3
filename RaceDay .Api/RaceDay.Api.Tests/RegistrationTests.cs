using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class RegistrationTests
{
    [Fact]
    public async Task Register_WithValidParticipantDetails_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            FirstName = "Thabo",
            LastName = "Test",
            Email = "thabo.registration.test@example.com",
            Password = "TestPassword123!",
            PhoneNumber = "0712345678",
            Role = "Participant"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            FirstName = "Thabo",
            LastName = "Test",
            Email = "not-an-email",
            Password = "TestPassword123!",
            PhoneNumber = "0712345678",
            Role = "Participant"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            FirstName = "Thabo",
            LastName = "Test",
            Email = "duplicate.registration.test@example.com",
            Password = "TestPassword123!",
            PhoneNumber = "0712345678",
            Role = "Participant"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }
    [Fact]
    public async Task Register_AsOrganiserWithoutInviteCode_ReturnsForbidden()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            FirstName = "Sipho",
            LastName = "Organiser",
            Email = "sipho.organiser.test@example.com",
            Password = "TestPassword123!",
            PhoneNumber = "0712345678",
            Role = "Organiser"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}