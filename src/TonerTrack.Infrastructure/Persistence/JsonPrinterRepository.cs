using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Repositories;

namespace TonerTrack.Infrastructure.Persistence;

public sealed class JsonPrinterRepository(
    IOptions<JsonPersistenceOptions> opts,
    ILogger<JsonPrinterRepository> logger)
    : IPrinterRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented  = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _dbFile = opts.Value.DatabaseFilePath;
    private readonly string _auditFile = opts.Value.AuditLogFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // IPrinterRepository implementation
    public async Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default)
    {
        var records = await LoadAsync(ct);
        return records.Values.Select(PrinterMapper.ToDomain).ToList();
    }

    public async Task<Printer?> GetByIpAsync(string ipAddress, CancellationToken ct = default)
    {
        var records = await LoadAsync(ct);
        return records.TryGetValue(ipAddress, out var r) ? PrinterMapper.ToDomain(r) : null;
    }

    public async Task AddAsync(Printer printer, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var records = await LoadAsync(ct);
            records[printer.IpAddress] = PrinterMapper.ToRecord(printer);
            await SaveAsync(records, ct);
            await AppendAuditAsync($"ADD ip={printer.IpAddress} name=\"{printer.Name}\"", ct);
        }
        finally { _lock.Release(); }
    }

    public async Task UpdateAsync(Printer printer, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var records = await LoadAsync(ct);
            records[printer.IpAddress] = PrinterMapper.ToRecord(printer);
            await SaveAsync(records, ct);
        }
        finally { _lock.Release(); }
    }

    public async Task DeleteAsync(string ipAddress, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var records = await LoadAsync(ct);
            if (records.Remove(ipAddress))
            {
                await SaveAsync(records, ct);
                await AppendAuditAsync($"DELETE ip={ipAddress}", ct);
            }
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> ExistsAsync(string ipAddress, CancellationToken ct = default)
    {
        var records = await LoadAsync(ct);
        return records.ContainsKey(ipAddress);
    }

    // Private helpers
    private async Task<Dictionary<string, PrinterRecord>> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_dbFile)) return [];

        try
        {
            await using var fs = new FileStream(
                _dbFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

            return await JsonSerializer.DeserializeAsync<Dictionary<string, PrinterRecord>>(
                       fs, JsonOpts, ct)
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load printer database from {Path}", _dbFile);
            return [];
        }
    }

    /// <summary>
    /// Atomic write: serialise to a temp file in the same directory, then
    /// replace the target with a single rename — no partial-write corruption.
    /// </summary>
    private async Task SaveAsync(Dictionary<string, PrinterRecord> records, CancellationToken ct)
    {
        var dir  = Path.GetDirectoryName(_dbFile)!;
        Directory.CreateDirectory(dir);

        var temp = Path.Combine(dir, $".tmp_{Path.GetRandomFileName()}");
        await using (var fs = new FileStream(
            temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(fs, records, JsonOpts, ct);
        }

        File.Move(temp, _dbFile, overwrite: true);
    }

    private async Task AppendAuditAsync(string entry, CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_auditFile)!);
            var line = $"[{DateTime.UtcNow:O}] {entry}{Environment.NewLine}";
            await File.AppendAllTextAsync(_auditFile, line, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write audit entry");
        }
    }
}

/// <summary>
/// Configuration options for JSON persistence. Database creation later.
/// </summary>
public sealed class JsonPersistenceOptions
{
    public const string Section = "Persistence";

    public string DataDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "data");
    public string DatabaseFileName { get; set; } = "printers.json";
    public string AuditLogFileName { get; set; } = "printers_audit.log";

    public string DatabaseFilePath => Path.Combine(DataDirectory, DatabaseFileName);
    public string AuditLogFilePath => Path.Combine(DataDirectory, AuditLogFileName);
}
