
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventRouteTests
{
    private static async Task AssertEventCreatedAsync(
        HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Event creation failed. " +
            $"Status: {(int)response.StatusCode} ({response.StatusCode}). " +
            $"Response: {body}");
    }

    [Fact]
    public async Task CreateEventRoute_AsOrganiser_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"routeorganiser_{Guid.NewGuid()}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Route",
                LastName = "Organiser",
                Email = email,
                Password = password,
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
                EventName = "Route Test Event",
                Description = "Testing event route creation.",
                EventType = "Running",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(2),
                Venue = "Test Stadium",
                City = "Johannesburg",
                Province = "Gauteng",
                RegistrationOpenDate = now.AddDays(-1),
                RegistrationCloseDate = now.AddMonths(1),
                MaxParticipants = 100
            });

        await AssertEventCreatedAsync(eventResponse);

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var routeResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/route",
            new
            {
                RouteName = "Johannesburg 5K",
                DistanceKm = 5.0m,
                StartLocation = "Test Stadium Gate",
                FinishLocation = "Test Stadium Finish",
                RouteUrl = "https://example.com/routes/johannesburg-5k"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            routeResponse.StatusCode);

        using var routeJson = JsonDocument.Parse(
            await routeResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            eventId,
            routeJson.RootElement
                .GetProperty("eventId")
                .GetInt32());

        Assert.Equal(
            5.0m,
            routeJson.RootElement
                .GetProperty("distanceKm")
                .GetDecimal());

        var getRouteResponse = await client.GetAsync(
            $"/api/events/{eventId}/route");

        Assert.Equal(
            HttpStatusCode.OK,
            getRouteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEventRoute_AsParticipant_ReturnsForbidden()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"participant_{Guid.NewGuid()}@example.com";

        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = email,
                Password = password,
                Role = "Participant"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

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

        var routeResponse = await client.PostAsJsonAsync(
            "/api/events/1/route",
            new
            {
                RouteName = "Unauthorized Route",
                DistanceKm = 5.0m,
                StartLocation = "Test Start",
                FinishLocation = "Test Finish",
                RouteUrl = "https://example.com/route"
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            routeResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEventRoute_TwiceForSameEvent_ReturnsConflict()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"routeorganiser_{Guid.NewGuid()}@example.com";

        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = email,
                Password = password,
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

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
                EventName = "Duplicate Route Test",
                Description = "Testing route duplication prevention.",
                EventType = "Running",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(2),
                Venue = "Test Stadium",
                City = "Johannesburg",
                Province = "Gauteng",
                RegistrationOpenDate = now.AddDays(-1),
                RegistrationCloseDate = now.AddMonths(1),
                MaxParticipants = 100
            });

        await AssertEventCreatedAsync(eventResponse);

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var firstRoute = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/route",
            new
            {
                RouteName = "First Route",
                DistanceKm = 5.0m,
                StartLocation = "Test Stadium Start",
                FinishLocation = "Test Stadium Finish",
                RouteUrl = (string?)null
            });

        Assert.Equal(
            HttpStatusCode.Created,
            firstRoute.StatusCode);

        var secondRoute = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/route",
            new
            {
                RouteName = "Second Route",
                DistanceKm = 10.0m,
                StartLocation = "Second Start",
                FinishLocation = "Second Finish",
                RouteUrl = (string?)null
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondRoute.StatusCode);
    }

    [Fact]
    public async Task UpdateEventRoute_AsOrganiser_ReturnsOkAndSavesChanges()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"routeupdate_{Guid.NewGuid()}@example.com";

        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = email,
                Password = password,
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

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
                EventName = "Route Update Test",
                Description = "Testing route updates.",
                EventType = "Running",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(2),
                Venue = "Test Stadium",
                City = "Johannesburg",
                Province = "Gauteng",
                RegistrationOpenDate = now.AddDays(-1),
                RegistrationCloseDate = now.AddMonths(1),
                MaxParticipants = 100
            });

        await AssertEventCreatedAsync(eventResponse);

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var createRouteResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/route",
            new
            {
                RouteName = "Original 5K Route",
                DistanceKm = 5.0m,
                StartLocation = "Original Start",
                FinishLocation = "Original Finish",
                RouteUrl = (string?)null
            });

        Assert.Equal(
            HttpStatusCode.Created,
            createRouteResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/events/{eventId}/route",
            new
            {
                RouteName = "Updated 10K Route",
                DistanceKm = 10.0m,
                StartLocation = "Updated Start",
                FinishLocation = "Updated Finish",
                RouteUrl = "https://example.com/routes/updated-10k"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/events/{eventId}/route");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        using var routeJson = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        var route = routeJson.RootElement;

        Assert.Equal(
            "Updated 10K Route",
            route.GetProperty("routeName").GetString());

        Assert.Equal(
            10.0m,
            route.GetProperty("distanceKm").GetDecimal());

        Assert.Equal(
            "Updated Start",
            route.GetProperty("startLocation").GetString());

        Assert.Equal(
            "Updated Finish",
            route.GetProperty("finishLocation").GetString());
    }

    [Fact]
    public async Task DeleteEventRoute_AsOrganiser_ReturnsNoContent()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"routedelete_{Guid.NewGuid()}@example.com";

        var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = email,
                Password = password,
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

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
                EventName = "Route Delete Test",
                Description = "Testing route deletion.",
                EventType = "Running",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(2),
                Venue = "Test Stadium",
                City = "Johannesburg",
                Province = "Gauteng",
                RegistrationOpenDate = now.AddDays(-1),
                RegistrationCloseDate = now.AddMonths(1),
                MaxParticipants = 100
            });

        await AssertEventCreatedAsync(eventResponse);

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var routeResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/route",
            new
            {
                RouteName = "Route to Delete",
                DistanceKm = 5.0m,
                StartLocation = "Test Start",
                FinishLocation = "Test Finish",
                RouteUrl = (string?)null
            });

        Assert.Equal(
            HttpStatusCode.Created,
            routeResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync(
            $"/api/events/{eventId}/route");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/events/{eventId}/route");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotUpdateOrDeleteRoute_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();

        using var ownerClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";

        var ownerEmail = $"owner_{Guid.NewGuid()}@example.com";

        var ownerRegistration = await ownerClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Route",
                LastName = "Owner",
                Email = ownerEmail,
                Password = password,
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            ownerRegistration.StatusCode);

        var ownerLogin = await ownerClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = ownerEmail,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            ownerLogin.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await ownerClient.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Route Ownership Test",
                Description = "Testing route ownership permissions.",
                EventType = "Running",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(2),
                Venue = "Test Stadium",
                City = "Johannesburg",
                Province = "Gauteng",
                RegistrationOpenDate = now.AddDays(-1),
                RegistrationCloseDate = now.AddMonths(1),
                MaxParticipants = 100
            });

        await AssertEventCreatedAsync(eventResponse);

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var routeResponse = await ownerClient.PostAsJsonAsync(
            $"/api/events/{eventId}/route",
            new
            {
                RouteName = "Owner's Route",
                DistanceKm = 5.0m,
                StartLocation = "Test Start",
                FinishLocation = "Test Finish",
                RouteUrl = (string?)null
            });

        Assert.Equal(
            HttpStatusCode.Created,
            routeResponse.StatusCode);

        using var otherClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var otherEmail = $"other_{Guid.NewGuid()}@example.com";

        var otherRegistration = await otherClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Other",
                LastName = "Organiser",
                Email = otherEmail,
                Password = password,
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            otherRegistration.StatusCode);

        var otherLogin = await otherClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = otherEmail,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            otherLogin.StatusCode);

        var updateResponse = await otherClient.PutAsJsonAsync(
            $"/api/events/{eventId}/route",
            new
            {
                RouteName = "Unauthorized Update",
                DistanceKm = 10.0m,
                StartLocation = "Other Start",
                FinishLocation = "Other Finish",
                RouteUrl = (string?)null
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            updateResponse.StatusCode);

        var deleteResponse = await otherClient.DeleteAsync(
            $"/api/events/{eventId}/route");

        Assert.Equal(
            HttpStatusCode.NotFound,
            deleteResponse.StatusCode);
    }
}
