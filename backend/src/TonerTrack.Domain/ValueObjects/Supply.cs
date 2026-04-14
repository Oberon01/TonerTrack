 namespace TonerTrack.Domain.ValueObjects;

public enum SupplyCategory { 
    TonerCartridge, 
    DrumUnit, 
    Other 
}

public sealed record Supply(string Name, SupplyLevel Level, SupplyCategory Category)
{
    public static SupplyCategory CategorizeByName(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("toner")) return SupplyCategory.TonerCartridge;
        if (lower.Contains("cartridge")) return SupplyCategory.TonerCartridge;
        if (lower.Contains("drum"))  return SupplyCategory.DrumUnit;
        return SupplyCategory.Other;
    }
}
