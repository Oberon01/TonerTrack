using System.Collections.Generic;

namespace TonerTrack.Infrastructure.Discovery;

public sealed class DiscoveryOptions
{
    public const string Section = "Discovery";
    public PrintServerOptions PrintServer { get; set; } = new();
    public NetworkScanOptions NetworkScan { get; set; } = new();
}

public sealed class PrintServerOptions
{
    public bool Enabled { get; set; } = true;
    public List<string> ServerNames { get; set; } = [];
    public List<ScanRange> LocationRanges { get; set; } = [];
}

public sealed class NetworkScanOptions
{
    public bool Enabled { get; set; } = true;
    public List<ScanRange> Ranges { get; set; } = [];
    public string Community { get; set; } = "public";
    public int TimeoutMs { get; set; } = 1000;
    public int MaxParallelism { get; set; } = 50;
}

public sealed class ScanRange
{
    public string Subnet { get; set; } = "";
    public int Cidr { get; set; } = 24;
    public string Location { get; set; } = "";
    public string Name { get; set; } = "";
}