using FluentAssertions;
using TaskManager.Application.Common;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Tests.Persistence;

public sealed class SqliteUserRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _db = new();
    private readonly SqliteUserRepository _sut;

    public SqliteUserRepositoryTests()
    {
        _sut = new SqliteUserRepository(_db.ConnectionFactory);
    }

    public Task InitializeAsync() => _db.InitializeAsync();

    public Task DisposeAsync() => _db.DisposeAsync();

    private static User NewUser(string username = "jane", string? email = null) =>
        User.Create(username, email ?? $"{username}@example.com", "hash-value", Now);

    [Fact]
    public async Task Add_then_GetById_round_trips_every_field()
    {
        var user = NewUser();

        await _sut.AddAsync(user);
        var loaded = await _sut.GetByIdAsync(user.Id);

        loaded.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task GetById_returns_null_when_missing()
    {
        (await _sut.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetByUsername_is_case_insensitive()
    {
        var user = NewUser("Jane");
        await _sut.AddAsync(user);

        var loaded = await _sut.GetByUsernameAsync("JANE");

        loaded!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByUsername_returns_null_when_missing()
    {
        (await _sut.GetByUsernameAsync("nobody")).Should().BeNull();
    }

    [Fact]
    public async Task EmailExists_reflects_stored_users()
    {
        await _sut.AddAsync(NewUser());

        (await _sut.EmailExistsAsync("JANE@example.com")).Should().BeTrue();
        (await _sut.EmailExistsAsync("other@example.com")).Should().BeFalse();
    }

    [Fact]
    public async Task Add_with_duplicate_username_in_different_case_throws_conflict()
    {
        await _sut.AddAsync(NewUser("jane"));

        var act = () => _sut.AddAsync(NewUser("JANE", "another@example.com"));

        await act.Should().ThrowAsync<ConflictException>();
        (await _db.CountAsync("Users")).Should().Be(1);
    }

    [Fact]
    public async Task Add_with_duplicate_email_throws_conflict()
    {
        await _sut.AddAsync(NewUser("jane", "same@example.com"));

        var act = () => _sut.AddAsync(NewUser("john", "same@example.com"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Username_containing_sql_is_stored_as_plain_data()
    {
        // Not a valid username for the domain, so build it via Rehydrate to hit the SQL directly.
        var hostile = User.Rehydrate(Guid.NewGuid(), "x'; DROP TABLE Users; --", "evil@example.com", "hash", Now);

        await _sut.AddAsync(hostile);

        (await _sut.GetByUsernameAsync("x'; DROP TABLE Users; --")).Should().NotBeNull();
        (await _db.CountAsync("Users")).Should().Be(1);
    }
}
