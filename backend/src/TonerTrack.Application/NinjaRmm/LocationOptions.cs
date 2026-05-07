namespace TonerTrack.Application.NinjaRmm;

public sealed class LocationOptions
{
    public const string Section = "Locations";
    public Dictionary<string, string> Names { get; set; } = [];
    public string GetName(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "";
        return Names.TryGetValue(id, out var name) ? name : id;
    }
}