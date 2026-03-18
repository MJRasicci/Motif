namespace Motif.Models;

using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;

/// <summary>
/// Exact score time expressed as a reduced fraction of a whole note.
/// </summary>
public readonly struct ScoreTime : IComparable<ScoreTime>, IEquatable<ScoreTime>
{
    /// <summary>
    /// Gets the zero duration.
    /// </summary>
    public static ScoreTime Zero => new(0, 1);

    /// <summary>
    /// Gets the numerator of the reduced whole-note fraction.
    /// </summary>
    public long Numerator { get; }

    /// <summary>
    /// Gets the denominator of the reduced whole-note fraction.
    /// </summary>
    public long Denominator { get; }

    /// <summary>
    /// Creates an exact score-time value from a whole-note fraction.
    /// </summary>
    [JsonConstructor]
    public ScoreTime(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), "Score-time denominator cannot be zero.");
        }

        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        if (numerator == 0)
        {
            Numerator = 0;
            Denominator = 1;
            return;
        }

        var divisor = (long)BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        Numerator = numerator / divisor;
        Denominator = denominator / divisor;
    }

    /// <summary>
    /// Parses an integer, decimal, or fractional score-time literal.
    /// </summary>
    public static ScoreTime Parse(string value)
    {
        if (!TryParse(value, out var result))
        {
            throw new FormatException($"'{value}' is not a valid score-time literal.");
        }

        return result;
    }

    /// <summary>
    /// Tries to parse an integer, decimal, or fractional score-time literal.
    /// </summary>
    public static bool TryParse(string? value, out ScoreTime result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = Zero;
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Contains('/', StringComparison.Ordinal))
        {
            var parts = trimmed.Split('/', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var numerator)
                && long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var denominator)
                && denominator != 0)
            {
                result = new ScoreTime(numerator, denominator);
                return true;
            }

            result = Zero;
            return false;
        }

        var sign = 1L;
        if (trimmed.StartsWith("-", StringComparison.Ordinal))
        {
            sign = -1;
            trimmed = trimmed[1..];
        }
        else if (trimmed.StartsWith("+", StringComparison.Ordinal))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.Contains(".", StringComparison.Ordinal))
        {
            var parts = trimmed.Split('.', 2, StringSplitOptions.None);
            if (parts.Length != 2
                || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var wholePart)
                || !long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fractionalPart))
            {
                result = Zero;
                return false;
            }

            var denominator = Pow10(parts[1].Length);
            var numerator = (wholePart * denominator) + fractionalPart;
            result = new ScoreTime(sign * numerator, denominator);
            return true;
        }

        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integral))
        {
            result = new ScoreTime(sign * integral, 1);
            return true;
        }

        result = Zero;
        return false;
    }

    /// <summary>
    /// Creates an exact score-time value from a decimal representation.
    /// </summary>
    public static ScoreTime FromDecimal(decimal value)
        => Parse(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Multiplies the score-time value by an exact ratio.
    /// </summary>
    public ScoreTime Multiply(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), "Score-time multiplier denominator cannot be zero.");
        }

        return new ScoreTime(Numerator * numerator, Denominator * denominator);
    }

    /// <summary>
    /// Converts the score-time value to a decimal approximation.
    /// </summary>
    public decimal ToDecimal()
        => Numerator / (decimal)Denominator;

    /// <summary>
    /// Formats the score-time value as a decimal string for score formats that require it.
    /// </summary>
    public string ToDecimalString(int maxDecimalPlaces = 6)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxDecimalPlaces);

        var value = Math.Round(ToDecimal(), maxDecimalPlaces, MidpointRounding.AwayFromZero);
        return value.ToString($"0.{new string('#', maxDecimalPlaces)}", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override string ToString()
        => Denominator == 1
            ? Numerator.ToString(CultureInfo.InvariantCulture)
            : $"{Numerator.ToString(CultureInfo.InvariantCulture)}/{Denominator.ToString(CultureInfo.InvariantCulture)}";

    /// <inheritdoc />
    public int CompareTo(ScoreTime other)
        => (BigInteger.Multiply(Numerator, other.Denominator))
            .CompareTo(BigInteger.Multiply(other.Numerator, Denominator));

    /// <inheritdoc />
    public bool Equals(ScoreTime other)
        => Numerator == other.Numerator && Denominator == other.Denominator;

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is ScoreTime other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Numerator, Denominator);

    public static ScoreTime operator +(ScoreTime left, ScoreTime right)
        => new(
            (left.Numerator * right.Denominator) + (right.Numerator * left.Denominator),
            left.Denominator * right.Denominator);

    public static ScoreTime operator -(ScoreTime left, ScoreTime right)
        => new(
            (left.Numerator * right.Denominator) - (right.Numerator * left.Denominator),
            left.Denominator * right.Denominator);

    public static bool operator ==(ScoreTime left, ScoreTime right)
        => left.Equals(right);

    public static bool operator !=(ScoreTime left, ScoreTime right)
        => !left.Equals(right);

    public static bool operator <(ScoreTime left, ScoreTime right)
        => left.CompareTo(right) < 0;

    public static bool operator <=(ScoreTime left, ScoreTime right)
        => left.CompareTo(right) <= 0;

    public static bool operator >(ScoreTime left, ScoreTime right)
        => left.CompareTo(right) > 0;

    public static bool operator >=(ScoreTime left, ScoreTime right)
        => left.CompareTo(right) >= 0;

    private static long Pow10(int exponent)
    {
        long value = 1;
        for (var i = 0; i < exponent; i++)
        {
            value *= 10;
        }

        return value;
    }
}
