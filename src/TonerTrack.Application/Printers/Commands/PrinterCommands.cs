using MediatR;
using TonerTrack.Application.Common.Interfaces;
using TonerTrack.Application.Printers.DTOs;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Exceptions;
using TonerTrack.Domain.Repositories;

namespace TonerTrack.Application.Printers.Commands;

// Add printer
public sealed record AddPrinterCommand(
    string Name, string IpAddress, string Community = "public") : IRequest<PrinterDto>;

public sealed class AddPrinterHandler(IPrinterRepository repo)
    : IRequestHandler<AddPrinterCommand, PrinterDto>
{
    public async Task<PrinterDto> Handle(AddPrinterCommand cmd, CancellationToken ct)
    {
        if (await repo.ExistsAsync(cmd.IpAddress, ct))
            throw new PrinterDomainException($"A printer with IP '{cmd.IpAddress}' already exists.");

        var printer = Printer.Create(cmd.IpAddress, cmd.Name, cmd.Community);
        await repo.AddAsync(printer, ct);
        return PrinterDto.FromEntity(printer);
    }
}

// Update printer (name and/or community string)
public sealed record UpdatePrinterCommand(
    string IpAddress, string? Name, string? Community) : IRequest<PrinterDto>;

public sealed class UpdatePrinterHandler(IPrinterRepository repo)
    : IRequestHandler<UpdatePrinterCommand, PrinterDto>
{
    public async Task<PrinterDto> Handle(UpdatePrinterCommand cmd, CancellationToken ct)
    {
        var printer = await repo.GetByIpAsync(cmd.IpAddress, ct)
                      ?? throw new PrinterNotFoundException(cmd.IpAddress);

        if (cmd.Name is not null) printer.Rename(cmd.Name);
        if (cmd.Community is not null) printer.SetCommunity(cmd.Community);

        await repo.UpdateAsync(printer, ct);
        return PrinterDto.FromEntity(printer);
    }
}

// Delete printer
public sealed record DeletePrinterCommand(string IpAddress) : IRequest;

public sealed class DeletePrinterHandler(IPrinterRepository repo)
    : IRequestHandler<DeletePrinterCommand>
{
    public async Task Handle(DeletePrinterCommand cmd, CancellationToken ct)
    {
        if (!await repo.ExistsAsync(cmd.IpAddress, ct))
            throw new PrinterNotFoundException(cmd.IpAddress);

        await repo.DeleteAsync(cmd.IpAddress, ct);
    }
}

// Poll a single printer for SNMP data.
public sealed record PollPrinterCommand(string IpAddress) : IRequest<PrinterDto>;

public sealed class PollPrinterHandler(
    IPrinterRepository repo,
    ISnmpService snmp,
    IDomainEventDispatcher dispatcher)
    : IRequestHandler<PollPrinterCommand, PrinterDto>
{
    public async Task<PrinterDto> Handle(PollPrinterCommand cmd, CancellationToken ct)
    {
        var printer = await repo.GetByIpAsync(cmd.IpAddress, ct)
                      ?? throw new PrinterNotFoundException(cmd.IpAddress);

        var result = await snmp.PollPrinterAsync(printer.IpAddress, printer.Community, ct);

        if (result is null) 
            printer.RecordSnmpUnreachable();
        else 
            printer.ApplyPollResult(result);

        await repo.UpdateAsync(printer, ct);
        await dispatcher.DispatchAsync(printer.DomainEvents, ct);
        printer.ClearDomainEvents();

        return PrinterDto.FromEntity(printer);
    }
}

// Bulk import printers from a list of IP addresses, names, and optional community strings.

public sealed record ImportPrinterRequest(string IpAddress, string Name, string Community = "public");

public sealed record ImportPrintersCommand(
    IReadOnlyList<ImportPrinterRequest> Printers) : IRequest<ImportPrintersResult>;

public sealed record ImportPrintersResult(int Imported, int Skipped, int Total);

public sealed class ImportPrintersHandler(IPrinterRepository repo)
    : IRequestHandler<ImportPrintersCommand, ImportPrintersResult>
{
    public async Task<ImportPrintersResult> Handle(ImportPrintersCommand cmd, CancellationToken ct)
    {
        int imported = 0, skipped = 0;

        foreach (var req in cmd.Printers)
        {
            if (string.IsNullOrWhiteSpace(req.IpAddress) || string.IsNullOrWhiteSpace(req.Name))
                continue;

            if (await repo.ExistsAsync(req.IpAddress, ct)) { skipped++; continue; }

            await repo.AddAsync(Printer.Create(req.IpAddress, req.Name, req.Community), ct);
            imported++;
        }

        var total = (await repo.GetAllAsync(ct)).Count;
        return new ImportPrintersResult(imported, skipped, total);
    }
}

// @TODO: Add functionality to automatically grab printers from print server or network scan, and bulk import them.
//          This would likely be a separate service that runs on a schedule, and uses the existing AddPrinterCommand to add new printers.
//          It could also update existing printers if they are found with new information (e.g. name change).