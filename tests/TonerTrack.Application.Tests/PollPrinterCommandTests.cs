using FluentAssertions;
using Moq;
using TonerTrack.Application.Common.Interfaces;
using TonerTrack.Application.Printers.Commands;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Enums;
using TonerTrack.Domain.Events;
using TonerTrack.Domain.Exceptions;
using TonerTrack.Domain.Repositories;
using TonerTrack.Domain.ValueObjects;
using Xunit;

namespace TonerTrack.Application.Tests;

public sealed class PollPrinterCommandTests
{
    private readonly Mock<IPrinterRepository> _repo = new();
    private readonly Mock<ISnmpService> _snmp = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly Mock<IPrinterEnrichmentService> _enrichment = new();

    private PollPrinterHandler Handler() =>
        new(_repo.Object, _snmp.Object, _dispatcher.Object, _enrichment.Object);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SnmpReturnsResult_UpdatesPrinterAndReturnsDto()
    {
        var printer = Printer.Create("10.0.0.1", "Finance Printer");
        _repo.Setup(r => r.GetByIpAsync("10.0.0.1", default)).ReturnsAsync(printer);

        var pollResult = new PrinterPollResult(
            "Canon iR-ADV C5550", "SN001", 12_000,
            [new Supply("Black Toner", SupplyLevel.FromRaw(80, 100), SupplyCategory.TonerCartridge)],
            []);

        _snmp.Setup(s => s.PollPrinterAsync("10.0.0.1", "public", default))
             .ReturnsAsync(pollResult);

        var dto = await Handler().Handle(new PollPrinterCommand("10.0.0.1"), default);

        dto.Status.Should().Be(PrinterStatus.Ok);
        dto.Model.Should().Be("Canon iR-ADV C5550");
        dto.TonerCartridges.Should().ContainKey("Black Toner")
           .WhoseValue.Should().Be("80%");

        _repo.Verify(r => r.UpdateAsync(printer, default), Times.Once);
    }

    [Fact]
    public async Task Handle_SnmpReturnsResult_DispatchesDomainEvents()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        _repo.Setup(r => r.GetByIpAsync("10.0.0.1", default)).ReturnsAsync(printer);

        // Low toner will cause a domain event to be raised
        var pollResult = new PrinterPollResult(
            "Model", "SN", 5_000,
            [new Supply("Black Toner", SupplyLevel.FromRaw(8, 100), SupplyCategory.TonerCartridge)],
            []);

        _snmp.Setup(s => s.PollPrinterAsync("10.0.0.1", "public", default))
             .ReturnsAsync(pollResult);

        await Handler().Handle(new PollPrinterCommand("10.0.0.1"), default);

        _dispatcher.Verify(
            d => d.DispatchAsync(
                It.IsAny<IReadOnlyList<IDomainEvent>>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SnmpReturnsResult_ClearsDomainEventsAfterDispatch()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        _repo.Setup(r => r.GetByIpAsync("10.0.0.1", default)).ReturnsAsync(printer);

        _snmp.Setup(s => s.PollPrinterAsync("10.0.0.1", "public", default))
             .ReturnsAsync(new PrinterPollResult("M", "S", 1_000, [], []));

        await Handler().Handle(new PollPrinterCommand("10.0.0.1"), default);

        // After the handler completes the aggregate should have no pending events
        printer.DomainEvents.Should().BeEmpty();
    }

    // ── SNMP unreachable ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SnmpUnreachable_IncrementsOfflineAttempts()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        _repo.Setup(r => r.GetByIpAsync("10.0.0.1", default)).ReturnsAsync(printer);
        _snmp.Setup(s => s.PollPrinterAsync("10.0.0.1", "public", default))
             .ReturnsAsync((PrinterPollResult?)null);

        var dto = await Handler().Handle(new PollPrinterCommand("10.0.0.1"), default);

        dto.OfflineAttempts.Should().Be(1);
        _repo.Verify(r => r.UpdateAsync(printer, default), Times.Once);
    }

    [Fact]
    public async Task Handle_SnmpUnreachableThreeTimes_SetsStatusOffline()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        _repo.Setup(r => r.GetByIpAsync("10.0.0.1", default)).ReturnsAsync(printer);
        _snmp.Setup(s => s.PollPrinterAsync("10.0.0.1", "public", default))
             .ReturnsAsync((PrinterPollResult?)null);

        var handler = Handler();
        await handler.Handle(new PollPrinterCommand("10.0.0.1"), default);
        await handler.Handle(new PollPrinterCommand("10.0.0.1"), default);
        var dto = await handler.Handle(new PollPrinterCommand("10.0.0.1"), default);

        dto.Status.Should().Be(PrinterStatus.Offline);
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PrinterNotFound_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.GetByIpAsync("99.0.0.1", default))
             .ReturnsAsync((Printer?)null);

        var act = () => Handler().Handle(new PollPrinterCommand("99.0.0.1"), default);

        await act.Should().ThrowAsync<PrinterNotFoundException>();
    }
}
