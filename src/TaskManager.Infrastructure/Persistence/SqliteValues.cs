using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Explicit conversions between CLR values and their stored TEXT form.
/// Microsoft.Data.Sqlite would store a Guid as a BLOB by default, so we never rely on implicit mapping.
/// </summary>
internal static class SqliteValues
{
    private const string DateFormat = "yyyy-MM-dd";

    public static string ToDb(Guid value) => value.ToString("D");

    public static string ToDb(DateTimeOffset value) => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    public static object ToDb(DateOnly? value) =>
        value is { } date ? date.ToString(DateFormat, CultureInfo.InvariantCulture) : DBNull.Value;

    public static object ToDb(string? value) => value is null ? DBNull.Value : value;

    public static Guid ReadGuid(this SqliteDataReader reader, int ordinal) =>
        Guid.Parse(reader.GetString(ordinal));

    public static DateTimeOffset ReadDateTimeOffset(this SqliteDataReader reader, int ordinal) =>
        DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static DateOnly? ReadNullableDateOnly(this SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : DateOnly.ParseExact(reader.GetString(ordinal), DateFormat, CultureInfo.InvariantCulture);

    public static string? ReadNullableString(this SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    public static SqliteCommand With(this SqliteCommand command, string name, object value)
    {
        command.Parameters.AddWithValue(name, value);
        return command;
    }
}
