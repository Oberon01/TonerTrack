using FluentAssertions;
using Moq;
using TonerTrack.Application.Printers.Queries;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Enums;
using TonerTrack.Domain.Repositories;
using TonerTrack.Domain.ValueObjects;
using Xunit;

namespace TonerTrack.Application.Tests;

public sealed class PrinterQueryTests
{
    // ── Stats ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStats_ReturnsCorrectCounts()
    {
        var printers = new List<Printer>
        {
            BuildPrinter("10.0.0.1", PrinterStatus.Ok),
            BuildPrinter("10.0.0.2", PrinterStatus.Ok),
            BuildPrinter("10.0.0.3", PrinterStatus.Warning),
            BuildPrinter("10.0.0.4", PrinterStatus.Error),
            BuildPrinter("10.0.0.5", PrinterStatus.Offline),
        };

        var repo = new Mock<IPrinterRepository>();
        repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(printers);

        var stats = await new GetPrinterStatsHandler(repo.Object)
            .Handle(new GetPrinterStatsQuery(), default);

        stats.Total.Should().Be(5);
        stats.Ok.Should().Be(2);
        stats.Warning.Should().Be(1);
        stats.Error.Should().Be(1);
        stats.Offline.Should().Be(1);
        stats.Unknown.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_EmptyRepository_ReturnsAllZeros()
    {
        var repo = new Mock<IPrinterRepository>();
        repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);

        var stats = await new GetPrinterStatsHandler(repo.Object)
            .Handle(new GetPrinterStatsQuery(), default);

        stats.Total.Should().Be(0);
        stats.Ok.Should().Be(0);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_MapsEntitiesToDtos()
    {
        var printers = new List<Printer>
        {
            Printer.Create("10.0.0.1", "Printer A"),
            Printer.Create("10.0.0.2", "Printer B"),
        };

        var repo = new Mock<IPrinterRepository>();
        repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(printers);

        var dtos = await new GetAllPrintersHandler(repo.Object)
            .Handle(new GetAllPrintersQuery(), default);

        dtos.Should().HaveCount(2);
        dtos.Select(d => d.IpAddress).Should().BeEquivalentTo(["10.0.0.1", "10.0.0.2"]);
    }

    // ── GetByIp ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIp_ExistingPrinter_ReturnsDto()
    {
        var printer = Printer.Create("10.0.0.1", "Finance");
        var repo    = new Mock<IPrinterRepository>();
        repo.Setup(r => r.GetByIpAsync("10.0.0.1", default)).ReturnsAsync(printer);

        var dto = await new GetPrinterByIpHandler(repo.Object)
            .Handle(new GetPrinterByIpQuery("10.0.0.1"), default);

        dto.IpAddress.Should().Be("10.0.0.1");
        dto.Name.Should().Be("Finance");
    }

    [Fact]
    public async Task GetByIp_NotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<IPrinterRepository>();
        repo.Setup(r => r.GetByIpAsync("99.0.0.1", default)).ReturnsAsync((Printer?)null);

        var act = () => new GetPrinterByIpHandler(repo.Object)
            .Handle(new GetPrinterByIpQuery("99.0.0.1"), default);

        await act.Should().ThrowAsync<Domain.Exceptions.PrinterNotFoundException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Build a Printer and force its status via RestoreFromPersistence so we
    /// can test stat counts without having to simulate an SNMP poll cycle.
    /// </summary>
    private static Printer BuildPrinter(string ip, PrinterStatus status)
    {
        var p = Printer.Create(ip, $"Printer_{ip}");

        var tonerPct = status switch
        {
            PrinterStatus.Error => 5,
            PrinterStatus.Warning => 15,
            PrinterStatus.Ok => 80,
            _ => 80
        };

        if (status == PrinterStatus.Offline)
        {
            p.RecordSnmpUnreachable();
            p.RecordSnmpUnreachable();
            p.RecordSnmpUnreachable();
        }
        else
        {
            p.ApplyPollResult(new PrinterPollResult(
                "Model", "SN", null,
                [new Supply("Black Toner", SupplyLevel.FromRaw(tonerPct, 100), SupplyCategory.TonerCartridge)],
                []));
        }

        p.ClearDomainEvents();
        return p;
    }
}
