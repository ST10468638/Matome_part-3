using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class LoginTests
{
    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var registerRequest = new
        {
            FirstName = "Thabo",
            LastName = "Test",
            Email = "thabo.login.test@example.com",
            Password = "CorrectPassword123!",
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
            Email = "thabo.login.test@example.com",
            Password = "WrongPassword123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode);
    }
    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var registerRequest = new
        {
            FirstName = "Ayanda",
            LastName = "Test",
            Email = "ayanda.login.test@example.com",
            Password = "CorrectPassword123!",
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
            Email = "ayanda.login.test@example.com",
            Password = "CorrectPassword123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);
    }
    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            Email = "unknown.login.test@example.com",
            Password = "SomePassword123!"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}