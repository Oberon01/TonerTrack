using System.Text.Json.Serialization;

namespace TonerTrack.Infrastructure.Persistence;

/// <summary>
/// JSON serialisation model for a printer record.
/// </summary>
internal sealed class PrinterRecord
{
    [JsonPropertyName("ip")] public string IpAddress { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("community")] public string Community { get; set; } = "public";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("user_overridden")] public bool UserOverridden { get; set; }
    [JsonPropertyName("model")] public string Model { get; set; } = "N/A";
    [JsonPropertyName("serial")] public string SerialNumber { get; set; } = "N/A";
    [JsonPropertyName("status")] public string Status { get; set; } = "Unknown";
    [JsonPropertyName("timestamp")] public DateTime? LastPolledAt { get; set; }
    [JsonPropertyName("total_pages")] public long? TotalPagesPrinted { get; set; }
    [JsonPropertyName("last_total_pages")] public long? LastTotalPages { get; set; }
    [JsonPropertyName("offline_attempts")] public int OfflineAttempts { get; set; }
    [JsonPropertyName("toner_cartridges")] public Dictionary<string, string> TonerCartridges { get; set; } = [];
    [JsonPropertyName("drum_units")] public Dictionary<string, string> DrumUnits { get; set; } = [];
    [JsonPropertyName("other")] public Dictionary<string, string> OtherSupplies { get; set; } = [];
    [JsonPropertyName("pages_history")] public Dictionary<string, long> PagesHistory { get; set; } = [];
}
