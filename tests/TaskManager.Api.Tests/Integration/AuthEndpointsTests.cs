using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Auth;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.Tests.Integration;

public class AuthEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string UniqueName() => $"user{Guid.NewGuid():N}"[..20];

    [Fact]
    public async Task Health_is_public()
    {
        var response = await factory.CreateClient().GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Seeded_demo_user_can_log_in()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DataSeeder.DemoUsername, DataSeeder.DemoPassword),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(ApiFactory.Json);
        auth!.Token.Should().NotBeNullOrEmpty();
        auth.User.Username.Should().Be(DataSeeder.DemoUsername);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_problem_details()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DataSeeder.DemoUsername, "WrongPass1"),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Register_then_call_a_protected_endpoint_with_the_returned_token()
    {
        var client = factory.CreateClient();
        var username = UniqueName();

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(username, $"{username}@example.com", "Passw0rd!"),
            ApiFactory.Json);

        register.StatusCode.Should().Be(HttpStatusCode.Created);
        register.Headers.Location!.ToString().Should().EndWith("/api/users/me");
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(ApiFactory.Json);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me", ApiFactory.Json);
        me!.Username.Should().Be(username);
    }

    [Fact]
    public async Task Register_with_existing_username_returns_409()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("DEMO", "another@example.com", "Passw0rd!"),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_400_with_field_errors()
    {
        var username = UniqueName();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(username, $"{username}@example.com", "short"),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiFactory.Json);
        problem!.Errors.Should().ContainKey("password");
    }

    [Fact]
    public async Task Protected_endpoint_without_token_returns_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Protected_endpoint_with_a_forged_token_returns_401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.real-token");

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
