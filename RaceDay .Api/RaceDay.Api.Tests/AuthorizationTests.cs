
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class AuthorizationTests
{
    [Fact]
    public async Task GetEvents_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var now = DateTime.UtcNow;

        var request = new
        {
            EventName = "Test Running Event",
            Description = "Automated authorization test",
            EventType = "Running",
            DistanceKm = 5.0m,
            EventDate = now.AddMonths(2),
            Venue = "Test Stadium",
            City = "Johannesburg",
            Province = "Gauteng",
            RegistrationOpenDate = now,
            RegistrationCloseDate = now.AddMonths(1),
            MaxParticipants = 100
        };

        var response = await client.PostAsJsonAsync(
            "/api/events",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var email = $"participant_{Guid.NewGuid()}@example.com";
        const string password = "TestPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Thabo",
                LastName = "Participant",
                Email = email,
                Password = password,
                PhoneNumber = "0712345678",
                Role = "Participant"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var now = DateTime.UtcNow;

        var eventRequest = new
        {
            EventName = "Participant Must Not Create This",
            Description = "Role authorization test",
            EventType = "Running",
            DistanceKm = 5.0m,
            EventDate = now.AddMonths(2),
            Venue = "Test Stadium",
            City = "Johannesburg",
            Province = "Gauteng",
            RegistrationOpenDate = now,
            RegistrationCloseDate = now.AddMonths(1),
            MaxParticipants = 100
        };

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            eventRequest);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            eventResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var email = $"organiser_{Guid.NewGuid()}@example.com";
        const string password = "TestPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = email,
                Password = password,
                PhoneNumber = "0712345678",
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Test Running Event",
                Description = "An event created by an Organiser test.",
                EventType = "Running",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(2),
                Venue = "Test Stadium",
                City = "Johannesburg",
                Province = "Gauteng",
                RegistrationOpenDate = now,
                RegistrationCloseDate = now.AddMonths(1),
                MaxParticipants = 100
            });

        Assert.Equal(
            HttpStatusCode.Created,
            eventResponse.StatusCode);
    }
}
