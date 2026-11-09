
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class ResultTests
{
    private static async Task AssertCreatedAsync(
        HttpResponseMessage response,
        string operation)
    {
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"{operation} failed. " +
            $"Status: {(int)response.StatusCode} ({response.StatusCode}). " +
            $"Response: {body}");
    }

    [Fact]
    public async Task RecordResult_AsOrganiser_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();

        using var organiserClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";

        var organiserEmail =
            $"organiser_{Guid.NewGuid()}@example.com";

        var organiserRegistration =
            await organiserClient.PostAsJsonAsync(
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

        var organiserLogin =
            await organiserClient.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = organiserEmail,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            organiserLogin.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse =
            await organiserClient.PostAsJsonAsync(
                "/api/events",
                new
                {
                    EventName = "Results Test Event",
                    Description = "Testing race result recording.",
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

        await AssertCreatedAsync(eventResponse, "Event creation");

        using var eventJson = JsonDocument.Parse(
            await eventResponse.Content.ReadAsStringAsync());

        var eventId = eventJson.RootElement
            .GetProperty("eventId")
            .GetInt32();

        var categoryResponse =
            await organiserClient.PostAsJsonAsync(
                $"/api/events/{eventId}/categories",
                new
                {
                    CategoryName = "Open Category",
                    CategoryType = "Age",
                    Description = "Results testing category.",
                    MinimumAge = 18,
                    MaximumAge = 39,
                    EntryFee = 100.00m,
                    Capacity = 50
                });

        await AssertCreatedAsync(categoryResponse, "Category creation");

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

        var participantRegistration =
            await participantClient.PostAsJsonAsync(
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

        var participantLogin =
            await participantClient.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = participantEmail,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            participantLogin.StatusCode);

        var enrolmentResponse =
            await participantClient.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    CategoryId = categoryId
                });

        await AssertCreatedAsync(enrolmentResponse, "Enrolment creation");

        using var enrolmentJson = JsonDocument.Parse(
            await enrolmentResponse.Content.ReadAsStringAsync());

        var enrolmentId = enrolmentJson.RootElement
            .GetProperty("enrolmentId")
            .GetInt32();

        var resultResponse =
            await organiserClient.PostAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/result",
                new
                {
                    FinishTime = "00:45:30",
                    OverallPosition = 1,
                    CategoryPosition = 1,
                    ResultStatus = "Finished"
                });

        await AssertCreatedAsync(resultResponse, "Result recording");

        using var resultJson = JsonDocument.Parse(
            await resultResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            enrolmentId,
            resultJson.RootElement
                .GetProperty("enrolmentId")
                .GetInt32());

        Assert.Equal(
            1,
            resultJson.RootElement
                .GetProperty("overallPosition")
                .GetInt32());

        Assert.Equal(
            "Finished",
            resultJson.RootElement
                .GetProperty("resultStatus")
                .GetString());
    }

    [Fact]
    public async Task GetMyResults_AfterResultIsRecorded_ReturnsOwnResult()
    {
        using var factory = new CustomWebApplicationFactory();

        using var organiserClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";

        var organiserEmail =
            $"organiser_{Guid.NewGuid()}@example.com";

        var organiserRegistration =
            await organiserClient.PostAsJsonAsync(
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

        Assert.Equal(
            HttpStatusCode.OK,
            organiserLogin.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await organiserClient.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Participant Results Test",
                Description = "Testing personal race results.",
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

        await AssertCreatedAsync(eventResponse, "Event creation");

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
                CategoryType = "Age",
                Description = "Test category.",
                MinimumAge = 18,
                MaximumAge = 39,
                EntryFee = 100.00m,
                Capacity = 50
            });

        await AssertCreatedAsync(categoryResponse, "Category creation");

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

        var participantRegistration =
            await participantClient.PostAsJsonAsync(
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

        var participantLogin =
            await participantClient.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = participantEmail,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            participantLogin.StatusCode);

        var enrolmentResponse =
            await participantClient.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    CategoryId = categoryId
                });

        await AssertCreatedAsync(enrolmentResponse, "Enrolment creation");

        using var enrolmentJson = JsonDocument.Parse(
            await enrolmentResponse.Content.ReadAsStringAsync());

        var enrolmentId = enrolmentJson.RootElement
            .GetProperty("enrolmentId")
            .GetInt32();

        var resultResponse = await organiserClient.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/result",
            new
            {
                FinishTime = "00:45:30",
                OverallPosition = 1,
                CategoryPosition = 1,
                ResultStatus = "Finished"
            });

        await AssertCreatedAsync(resultResponse, "Result recording");

        var myResultsResponse =
            await participantClient.GetAsync("/api/results/my");

        Assert.Equal(
            HttpStatusCode.OK,
            myResultsResponse.StatusCode);

        using var resultsJson = JsonDocument.Parse(
            await myResultsResponse.Content.ReadAsStringAsync());

        var results = resultsJson.RootElement;

        Assert.Equal(1, results.GetArrayLength());

        var participantResult = results[0];

        Assert.Equal(
            enrolmentId,
            participantResult.GetProperty("enrolmentId").GetInt32());

        Assert.Equal(
            "Finished",
            participantResult.GetProperty("resultStatus").GetString());

        Assert.Equal(
            1,
            participantResult.GetProperty("overallPosition").GetInt32());
    }

    [Fact]
    public async Task GetMyResults_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/results/my");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_AsParticipant_ReturnsForbidden()
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

        var response = await client.PostAsJsonAsync(
            "/api/enrolments/1/result",
            new
            {
                FinishTime = "00:45:30",
                OverallPosition = 1,
                CategoryPosition = 1,
                ResultStatus = "Finished"
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsOrganiser_ReturnsForbidden()
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

        var response = await client.GetAsync("/api/results/my");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}
