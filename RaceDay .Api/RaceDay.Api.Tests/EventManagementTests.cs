
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventManagementTests
{
    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnEvent()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"eventmanager_{Guid.NewGuid()}@example.com";

        // Step 1: Register the Organiser.
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

        // Step 2: Log in as the Organiser.
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

        // Step 3: Create the event.
        var createResponse = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Original Event Name",
                Description = "Testing event management.",
                EventType = "Running",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(2),
                Venue = "Original Stadium",
                City = "Johannesburg",
                Province = "Gauteng",
                RegistrationOpenDate = now.AddDays(-1),
                RegistrationCloseDate = now.AddMonths(1),
                MaxParticipants = 100
            });

        Assert.True(
            createResponse.StatusCode == HttpStatusCode.Created,
            $"Event creation failed. Status: {(int)createResponse.StatusCode}. " +
            $"Response: {await createResponse.Content.ReadAsStringAsync()}");

        using var eventJson = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        // Step 4: Update the event.
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/events/{eventId}",
            new
            {
                EventName = "Updated Event Name",
                Description = "The event has been updated.",
                EventType = "Cycling",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(3),
                Venue = "Updated Stadium",
                City = "Pretoria",
                Province = "Gauteng",
                RegistrationOpenDate = now.AddDays(-1),
                RegistrationCloseDate = now.AddMonths(2),
                MaxParticipants = 150
            });

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        // Step 5: Verify that the event changes were saved.
        var getResponse = await client.GetAsync(
            $"/api/events/{eventId}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        using var updatedEventJson = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        var updatedEvent = updatedEventJson.RootElement;

        Assert.Equal(
            "Updated Event Name",
            updatedEvent.GetProperty("eventName").GetString());

        Assert.Equal(
            "Cycling",
            updatedEvent.GetProperty("eventType").GetString());

        Assert.Equal(
            "Updated Stadium",
            updatedEvent.GetProperty("venue").GetString());

        // Step 6: Delete the event.
        var deleteResponse = await client.DeleteAsync(
            $"/api/events/{eventId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        // Step 7: Confirm the event no longer exists.
        var deletedEventResponse = await client.GetAsync(
            $"/api/events/{eventId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            deletedEventResponse.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotUpdateOrDeleteEvent()
    {
        using var factory = new CustomWebApplicationFactory();

        var clientOptions =
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            };

        using var ownerClient = factory.CreateClient(clientOptions);
        using var otherClient = factory.CreateClient(clientOptions);

        const string password = "TestPassword123!";

        // Register the event's owning Organiser.
        var ownerEmail = $"owner_{Guid.NewGuid()}@example.com";

        var ownerRegistration = await ownerClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Event",
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

        // The owner creates an event.
        var now = DateTime.UtcNow;

        var eventResponse = await ownerClient.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Ownership Security Test",
                Description = "Testing event ownership.",
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

        Assert.True(
            eventResponse.StatusCode == HttpStatusCode.Created,
            $"Event creation failed. Status: {(int)eventResponse.StatusCode}. " +
            $"Response: {await eventResponse.Content.ReadAsStringAsync()}");

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        // Register a different Organiser.
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

        // The other Organiser cannot update this event.
        var updateResponse = await otherClient.PutAsJsonAsync(
            $"/api/events/{eventId}",
            new
            {
                EventName = "Unauthorized Event Update",
                Description = "This change must not be saved.",
                EventType = "Running",
                DistanceKm = 5.0m,
                EventDate = now.AddMonths(2),
                Venue = "Other Stadium",
                City = "Pretoria",
                Province = "Gauteng",
                RegistrationOpenDate = now.AddDays(-1),
                RegistrationCloseDate = now.AddMonths(1),
                MaxParticipants = 100
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            updateResponse.StatusCode);

        // The other Organiser cannot delete this event.
        var deleteResponse = await otherClient.DeleteAsync(
            $"/api/events/{eventId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            deleteResponse.StatusCode);

        // The owner confirms their event remains unchanged.
        var verifyResponse = await ownerClient.GetAsync(
            $"/api/events/{eventId}");

        Assert.Equal(
            HttpStatusCode.OK,
            verifyResponse.StatusCode);

        using var verifyJson = JsonDocument.Parse(
            await verifyResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            "Ownership Security Test",
            verifyJson.RootElement
                .GetProperty("eventName")
                .GetString());
    }
}
