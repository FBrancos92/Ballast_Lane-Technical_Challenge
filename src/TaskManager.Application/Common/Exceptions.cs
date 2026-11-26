namespace TaskManager.Application.Common;

/// <summary>The requested resource does not exist, or does not belong to the caller.</summary>
public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");

/// <summary>The request conflicts with existing state, for example a username that is already taken.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Login failed. Deliberately vague so it does not reveal whether the username exists.</summary>
public sealed class InvalidCredentialsException() : Exception("Invalid username or password.");
