namespace TonerTrack.Domain.ValueObjects;

// Immutable value object representing the level of a consumable supply. Two SupplyLevels with the same percentage are equal.
public sealed record SupplyLevel
{
    public static readonly SupplyLevel Unknown = new("Unknown");
    public static readonly SupplyLevel Ok = new("OK");

    public string Display { get; }
    public int? Percentage { get; }

    private SupplyLevel(string display)
    {
        Display = display;
    }

    private SupplyLevel(int percentage)
    {
        if (percentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be 0–100.");

        Percentage = percentage;
        Display = $"{percentage}%";
    }

    // Compute percentage from raw SNMP level/max values
    public static SupplyLevel FromRaw(int level, int maxLevel)
    {
        if (level == -2) return Unknown;
        if (level == -3) return Ok;
        if (maxLevel <= 0) return Unknown;

        var pct = (int)Math.Round((double)level / maxLevel * 100);
        return new SupplyLevel(Math.Clamp(pct, 0, 100));
    }

    // Rehydrate from a persisted display string such as "75%" or "Unknown"
    public static SupplyLevel FromDisplay(string? display)
    {
        if (string.IsNullOrWhiteSpace(display)) return Unknown;

        if (display.TrimEnd('%') is { } stripped && int.TryParse(stripped, out var pct))
            return new SupplyLevel(pct);

        return display switch
        {
            "Unknown" => Unknown,
            "OK" => Ok,
            _ => Unknown
        };
    }

    // True when toner is below 20% - drives Warning status.
    public bool IsLow => Percentage is not null && Percentage < 20;

    // True when toner is below 10% - drives Error status.
    public bool IsCritical => Percentage is not null && Percentage < 10;

    public override string ToString() => Display;
}
