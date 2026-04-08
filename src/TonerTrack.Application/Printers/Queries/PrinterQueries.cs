using MediatR;
using TonerTrack.Application.Printers.DTOs;
using TonerTrack.Domain.Enums;
using TonerTrack.Domain.Exceptions;
using TonerTrack.Domain.Repositories;

namespace TonerTrack.Application.Printers.Queries;

// Get all printers
public sealed record GetAllPrintersQuery : IRequest<IReadOnlyList<PrinterDto>>;

public sealed class GetAllPrintersHandler(IPrinterRepository repo)
    : IRequestHandler<GetAllPrintersQuery, IReadOnlyList<PrinterDto>>
{
    public async Task<IReadOnlyList<PrinterDto>> Handle(GetAllPrintersQuery _, CancellationToken ct)
    {
        var printers = await repo.GetAllAsync(ct);
        return printers.Select(PrinterDto.FromEntity).ToList();
    }
}

// Get single printer by IP address

public sealed record GetPrinterByIpQuery(string IpAddress) : IRequest<PrinterDto>;

public sealed class GetPrinterByIpHandler(IPrinterRepository repo)
    : IRequestHandler<GetPrinterByIpQuery, PrinterDto>
{
    public async Task<PrinterDto> Handle(GetPrinterByIpQuery request, CancellationToken ct)
    {
        var printer = await repo.GetByIpAsync(request.IpAddress, ct)
                      ?? throw new PrinterNotFoundException(request.IpAddress);
        return PrinterDto.FromEntity(printer);
    }
}

// Get printer stats
public sealed record GetPrinterStatsQuery : IRequest<PrinterStatsDto>;

public sealed class GetPrinterStatsHandler(IPrinterRepository repo)
    : IRequestHandler<GetPrinterStatsQuery, PrinterStatsDto>
{
    public async Task<PrinterStatsDto> Handle(GetPrinterStatsQuery _, CancellationToken ct)
    {
        var all = await repo.GetAllAsync(ct);
        return new PrinterStatsDto(
            Total: all.Count,
            Ok: all.Count(p => p.Status == PrinterStatus.Ok),
            Warning: all.Count(p => p.Status == PrinterStatus.Warning),
            Error: all.Count(p => p.Status == PrinterStatus.Error),
            Offline: all.Count(p => p.Status == PrinterStatus.Offline),
            Unknown: all.Count(p => p.Status == PrinterStatus.Unknown));
    }
}

// Get printer usage history and stats
public sealed record GetPrinterUsageQuery(string IpAddress) : IRequest<UsageDto>;

public sealed class GetPrinterUsageHandler(IPrinterRepository repo)
    : IRequestHandler<GetPrinterUsageQuery, UsageDto>
{
    public async Task<UsageDto> Handle(GetPrinterUsageQuery request, CancellationToken ct)
    {
        var printer = await repo.GetByIpAsync(request.IpAddress, ct) 
            ?? throw new PrinterNotFoundException(request.IpAddress);

        var history = printer.PagesHistory;
        var sorted = history.Keys.OrderByDescending(k => k).ToList();
        var last6 = sorted.Take(6).ToList();

        var last6Data = last6.Select(m => new MonthlyUsageDto(m, history[m])).ToList();
        long avg6 = last6.Count > 0
            ? (long)(last6.Sum(m => history[m]) / (double)last6.Count)
            : 0;

        long? lastMonth = last6.Count > 0 ? history[last6[0]] : null;
        double? changePct = null;
        if (last6.Count > 1)
        {
            var prev = history[last6[1]];
            if (prev > 0) changePct = Math.Round(((lastMonth!.Value - prev) / (double)prev) * 100, 1);
        }

        return new UsageDto(printer.IpAddress, printer.Name, last6Data, avg6, lastMonth, changePct, history);
    }
}
