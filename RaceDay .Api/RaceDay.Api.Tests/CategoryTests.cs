using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class CategoryTests
{
    [Fact]
    public async Task CreateCategory_AsOrganiser_DistanceCategory_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"distancecategory_{Guid.NewGuid()}@example.com";

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

        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "10K Distance Category Test",
                Description = "Testing a distance-based category.",
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

        var eventId = eventJson.RootElement.GetProperty("eventId").GetInt32();

        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new
            {
                CategoryName = "10 Kilometre Race",
                Description = "Distance-based running category.",
                CategoryType = "Distance",
                DistanceKm = 10.0m,
                EntryFee = 150.00m,
                Capacity = 100
            });

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        using var categoryJson = JsonDocument.Parse(
            await categoryResponse.Content.ReadAsStringAsync());

        var category = categoryJson.RootElement;

        Assert.Equal("Distance", category.GetProperty("categoryType").GetString());
        Assert.Equal(10.0m, category.GetProperty("distanceKm").GetDecimal());
        Assert.Equal("10 Kilometre Race", category.GetProperty("categoryName").GetString());
        Assert.Equal(JsonValueKind.Null, category.GetProperty("minimumAge").ValueKind);
        Assert.Equal(JsonValueKind.Null, category.GetProperty("maximumAge").ValueKind);

        var categoryId = category.GetProperty("categoryId").GetInt32();

        var getResponse = await client.GetAsync(
            $"/api/events/{eventId}/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var savedCategoryJson = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            10.0m,
            savedCategoryJson.RootElement.GetProperty("distanceKm").GetDecimal());
    }

    [Fact]
    public async Task CreateCategory_DistanceCategoryWithoutDistance_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"invalidcategory_{Guid.NewGuid()}@example.com";

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

        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Invalid Category Test Event",
                Description = "Testing category validation.",
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

        var eventId = eventJson.RootElement.GetProperty("eventId").GetInt32();

        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new
            {
                CategoryName = "Missing Distance",
                Description = "This category has no distance.",
                CategoryType = "Distance",
                EntryFee = 100.00m,
                Capacity = 50
            });

        Assert.Equal(HttpStatusCode.BadRequest, categoryResponse.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnCategory()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"categorymanager_{Guid.NewGuid()}@example.com";

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

        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Category Management Test",
                Description = "Testing category update and deletion.",
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

        var eventId = eventJson.RootElement.GetProperty("eventId").GetInt32();

        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new
            {
                CategoryName = "Original Age Category",
                Description = "Original category description.",
                CategoryType = "Age",
                MinimumAge = 18,
                MaximumAge = 39,
                EntryFee = 100.00m,
                Capacity = 50
            });

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        using var categoryJson = JsonDocument.Parse(
            await categoryResponse.Content.ReadAsStringAsync());

        var categoryId = categoryJson.RootElement.GetProperty("categoryId").GetInt32();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/events/{eventId}/categories/{categoryId}",
            new
            {
                CategoryName = "Updated Age Category",
                Description = "Updated category description.",
                CategoryType = "Age",
                MinimumAge = 21,
                MaximumAge = 45,
                EntryFee = 125.00m,
                Capacity = 75
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/events/{eventId}/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var updatedCategoryJson = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        var updatedCategory = updatedCategoryJson.RootElement;

        Assert.Equal(
            "Updated Age Category",
            updatedCategory.GetProperty("categoryName").GetString());

        Assert.Equal(21, updatedCategory.GetProperty("minimumAge").GetInt32());
        Assert.Equal(45, updatedCategory.GetProperty("maximumAge").GetInt32());
        Assert.Equal(125.00m, updatedCategory.GetProperty("entryFee").GetDecimal());

        var deleteResponse = await client.DeleteAsync(
            $"/api/events/{eventId}/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var deletedCategoryResponse = await client.GetAsync(
            $"/api/events/{eventId}/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.NotFound, deletedCategoryResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AgeCategoryWithInvalidAgeRange_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        const string password = "TestPassword123!";
        var email = $"invalidage_{Guid.NewGuid()}@example.com";

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

        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var now = DateTime.UtcNow;

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Age Validation Test",
                Description = "Testing category age validation.",
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

        var eventId = eventJson.RootElement.GetProperty("eventId").GetInt32();

        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new
            {
                CategoryName = "Invalid Age Category",
                Description = "Minimum age must not exceed maximum age.",
                CategoryType = "Age",
                MinimumAge = 50,
                MaximumAge = 20,
                EntryFee = 100.00m,
                Capacity = 50
            });

        Assert.Equal(HttpStatusCode.BadRequest, categoryResponse.StatusCode);
    }
}