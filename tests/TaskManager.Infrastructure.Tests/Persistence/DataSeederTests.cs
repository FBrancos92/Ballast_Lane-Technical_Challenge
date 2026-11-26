using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Security;

namespace TaskManager.Infrastructure.Tests.Persistence;

public sealed class DataSeederTests : IAsyncLifetime
{
    private readonly SqliteTestDatabase _db = new();
    private readonly SqliteUserRepository _users;
    private readonly DataSeeder _sut;
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 1_000);

    public DataSeederTests()
    {
        _users = new SqliteUserRepository(_db.ConnectionFactory);
        _sut = new DataSeeder(
            _users,
            new SqliteTaskRepository(_db.ConnectionFactory),
            _hasher,
            TimeProvider.System,
            NullLogger<DataSeeder>.Instance);
    }

    public Task InitializeAsync() => _db.InitializeAsync();

    public Task DisposeAsync() => _db.DisposeAsync();

    [Fact]
    public async Task Seed_creates_demo_users_whose_passwords_verify()
    {
        await _sut.SeedAsync();

        var demo = await _users.GetByUsernameAsync(DataSeeder.DemoUsername);
        demo.Should().NotBeNull();
        _hasher.Verify(DataSeeder.DemoPassword, demo!.PasswordHash).Should().BeTrue();

        var second = await _users.GetByUsernameAsync(DataSeeder.SecondUsername);
        _hasher.Verify(DataSeeder.SecondPassword, second!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Seed_creates_tasks()
    {
        await _sut.SeedAsync();

        (await _db.CountAsync("Tasks")).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Seed_runs_only_once()
    {
        await _sut.SeedAsync();
        var tasksAfterFirstRun = await _db.CountAsync("Tasks");

        await _sut.SeedAsync();

        (await _db.CountAsync("Users")).Should().Be(2);
        (await _db.CountAsync("Tasks")).Should().Be(tasksAfterFirstRun);
    }
}
