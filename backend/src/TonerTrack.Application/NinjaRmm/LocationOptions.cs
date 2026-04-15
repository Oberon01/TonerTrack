namespace TonerTrack.Application.NinjaRmm;

public sealed record LocationOptions
{
    public const string Section = "Locations";
    public Dictionary<string, string> Names { get; set; } = [];
    public string GetName(string? id) =>
        string.IsNullOrWhiteSpace(id)
        ? ""
        : Names.TryGetValue(id, out var name) ? name : id;
}