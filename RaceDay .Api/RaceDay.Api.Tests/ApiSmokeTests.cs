using System.Net;
using Xunit;

namespace RaceDay.Api.Tests;

public class ApiSmokeTests
{
    [Fact]
    public async Task Api_ShouldRespondToHttpRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}