using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class EnrolmentTests
{
    private static async Task CheckStatusAsync(
        HttpResponseMessage response,
        HttpStatusCode expected,
        string step)
    {
        if (response.StatusCode == expected)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == expected,
            $"{step} failed. " +
            $"Expected: {(int)expected} {expected}. " +
            $"Actual: {(int)response.StatusCode} {response.StatusCode}. " +
            $"Response body: {body}");
    }

    [Fact]
    public async Task CreateEnrolment_AsParticipant_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();

        using var organiserClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var organiserEmail = $"organiser_{Guid.NewGuid()}@example.com";

        var organiserRegistration = await organiserClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = organiserEmail,
                Password = password,
                PhoneNumber = "0712345678",
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            organiserRegistration.StatusCode);

        var organiserLogin = await organiserClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = organiserEmail,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, organiserLogin.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await organiserClient.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Enrolment Test Event",
                Description = "Testing Participant enrolment.",
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

        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new
            {
                CategoryName = "Open 18-39",
                Description = "Open running category.",
                CategoryType = "Age",
                MinimumAge = 18,
                MaximumAge = 39,
                EntryFee = 100.00m,
                Capacity = 50
            });

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        using var categoryJson = JsonDocument.Parse(
            await categoryResponse.Content.ReadAsStringAsync());

        var categoryId = categoryJson.RootElement
            .GetProperty("categoryId")
            .GetInt32();

        using var participantClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var participantEmail =
            $"participant_{Guid.NewGuid()}@example.com";

        var participantRegistration = await participantClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = participantEmail,
                Password = password,
                PhoneNumber = "0723456789",
                Role = "Participant"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            participantRegistration.StatusCode);

        var participantLogin = await participantClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = participantEmail,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, participantLogin.StatusCode);

        var enrolmentResponse = await participantClient.PostAsJsonAsync(
            "/api/enrolments",
            new
            {
                CategoryId = categoryId
            });

        Assert.Equal(
            HttpStatusCode.Created,
            enrolmentResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEnrolment_TwiceInSameCategory_ReturnsConflict()
    {
        using var factory = new CustomWebApplicationFactory();

        using var organiserClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var organiserEmail = $"organiser_{Guid.NewGuid()}@example.com";

        var registerOrganiser = await organiserClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = organiserEmail,
                Password = password,
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        await CheckStatusAsync(
            registerOrganiser,
            HttpStatusCode.Created,
            "Organiser registration");

        var loginOrganiser = await organiserClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = organiserEmail,
                Password = password
            });

        await CheckStatusAsync(
            loginOrganiser,
            HttpStatusCode.OK,
            "Organiser login");

        var now = DateTime.UtcNow;

        var eventResponse = await organiserClient.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Duplicate Enrolment Test",
                Description = "Testing duplicate enrolment prevention.",
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

        await CheckStatusAsync(
            eventResponse,
            HttpStatusCode.Created,
            "Event creation");

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new
            {
                CategoryName = "Open Category",
                Description = "Test category.",
                CategoryType = "Age",
                MinimumAge = 18,
                MaximumAge = 39,
                EntryFee = 100.00m,
                Capacity = 50
            });

        await CheckStatusAsync(
            categoryResponse,
            HttpStatusCode.Created,
            "Category creation");

        using var categoryJson = JsonDocument.Parse(
            await categoryResponse.Content.ReadAsStringAsync());

        var categoryId = categoryJson.RootElement
            .GetProperty("categoryId")
            .GetInt32();

        using var participantClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var participantEmail =
            $"participant_{Guid.NewGuid()}@example.com";

        var registerParticipant = await participantClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = participantEmail,
                Password = password,
                Role = "Participant"
            });

        await CheckStatusAsync(
            registerParticipant,
            HttpStatusCode.Created,
            "Participant registration");

        var loginParticipant = await participantClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = participantEmail,
                Password = password
            });

        await CheckStatusAsync(
            loginParticipant,
            HttpStatusCode.OK,
            "Participant login");

        var firstEnrolment = await participantClient.PostAsJsonAsync(
            "/api/enrolments",
            new
            {
                CategoryId = categoryId
            });

        await CheckStatusAsync(
            firstEnrolment,
            HttpStatusCode.Created,
            "First enrolment");

        var secondEnrolment = await participantClient.PostAsJsonAsync(
            "/api/enrolments",
            new
            {
                CategoryId = categoryId
            });

        await CheckStatusAsync(
            secondEnrolment,
            HttpStatusCode.Conflict,
            "Duplicate enrolment");
    }

    [Fact]
    public async Task GetMyEnrolments_AfterSuccessfulEnrolment_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory();

        using var organiserClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var organiserEmail = $"organiser_{Guid.NewGuid()}@example.com";

        var organiserRegistration = await organiserClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = organiserEmail,
                Password = password,
                Role = "Organiser",
                OrganiserInviteCode = "TestOrganiserInvite123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            organiserRegistration.StatusCode);

        var organiserLogin = await organiserClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = organiserEmail,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, organiserLogin.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await organiserClient.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "View Enrolments Test",
                Description = "Testing participant enrolment history.",
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

        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new
            {
                CategoryName = "Open Category",
                Description = "Test category.",
                CategoryType = "Age",
                MinimumAge = 18,
                MaximumAge = 39,
                EntryFee = 100.00m,
                Capacity = 50
            });

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        using var categoryJson = JsonDocument.Parse(
            await categoryResponse.Content.ReadAsStringAsync());

        var categoryId = categoryJson.RootElement
            .GetProperty("categoryId")
            .GetInt32();

        using var participantClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var participantEmail =
            $"participant_{Guid.NewGuid()}@example.com";

        var participantRegistration = await participantClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = participantEmail,
                Password = password,
                Role = "Participant"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            participantRegistration.StatusCode);

        var participantLogin = await participantClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = participantEmail,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, participantLogin.StatusCode);

        var enrolmentResponse = await participantClient.PostAsJsonAsync(
            "/api/enrolments",
            new
            {
                CategoryId = categoryId
            });

        Assert.Equal(
            HttpStatusCode.Created,
            enrolmentResponse.StatusCode);

        var myEnrolmentsResponse = await participantClient.GetAsync(
            "/api/enrolments/my");

        Assert.Equal(
            HttpStatusCode.OK,
            myEnrolmentsResponse.StatusCode);

        var enrolments =
            await myEnrolmentsResponse.Content.ReadFromJsonAsync<
                List<JsonElement>>();

        Assert.NotNull(enrolments);
        Assert.Single(enrolments);

        Assert.Equal(
            categoryId,
            enrolments[0].GetProperty("categoryId").GetInt32());

        Assert.Equal(
            eventId,
            enrolments[0].GetProperty("eventId").GetInt32());
    }

    [Fact]
    public async Task CreateEnrolment_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/enrolments",
            new
            {
                CategoryId = 1
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetEventEnrolments_AsOrganiser_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"organiser_{Guid.NewGuid()}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
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

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Organiser Enrolment List Test",
                Description = "Testing access to event enrolments.",
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

        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var response = await client.GetAsync(
            $"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var enrolments =
            await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(enrolments);
        Assert.Empty(enrolments);
    }

    [Fact]
    public async Task GetEventEnrolments_AsParticipant_ReturnsForbidden()
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

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = email,
                Password = password,
                Role = "Participant"
            });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var response = await client.GetAsync(
            "/api/events/1/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}