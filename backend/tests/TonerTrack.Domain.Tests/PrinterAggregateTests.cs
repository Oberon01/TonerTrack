using FluentAssertions;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Enums;
using TonerTrack.Domain.Events;
using TonerTrack.Domain.Exceptions;
using TonerTrack.Domain.ValueObjects;
using Xunit;

namespace TonerTrack.Domain.Tests;

public sealed class PrinterAggregateTests
{
    // ── Printer.Create ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArgs_InitialisesDefaults()
    {
        var printer = Printer.Create("10.0.0.1", "Finance Printer", "public");

        printer.IpAddress.Should().Be("10.0.0.1");
        printer.Name.Should().Be("Finance Printer");
        printer.Community.Should().Be("public");
        printer.Status.Should().Be(PrinterStatus.Unknown);
        printer.Supplies.Should().BeEmpty();
        printer.DomainEvents.Should().BeEmpty();
        printer.OfflineAttempts.Should().Be(0);
    }

    [Theory]
    [InlineData("", "Valid Name")]
    [InlineData("  ", "Valid Name")]
    [InlineData("10.0.0.1", "")]
    [InlineData("10.0.0.1", "   ")]
    public void Create_WithInvalidArgs_ThrowsDomainException(string ip, string name)
    {
        var act = () => Printer.Create(ip, name);
        act.Should().Throw<PrinterDomainException>();
    }

    // ── ApplyPollResult — status evaluation ───────────────────────────────────

    [Fact]
    public void ApplyPollResult_HealthyToner_SetsStatusOk()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(tonerPct: 80));

        printer.Status.Should().Be(PrinterStatus.Ok);
    }

    [Fact]
    public void ApplyPollResult_TonerAt19Percent_SetsStatusWarning()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(tonerPct: 19));

        printer.Status.Should().Be(PrinterStatus.Warning);
    }

    [Fact]
    public void ApplyPollResult_TonerAt20Percent_SetsStatusOk()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(tonerPct: 20));

        printer.Status.Should().Be(PrinterStatus.Ok);
    }

    [Fact]
    public void ApplyPollResult_TonerAt9Percent_SetsStatusError()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(tonerPct: 9));

        printer.Status.Should().Be(PrinterStatus.Error);
    }

    [Fact]
    public void ApplyPollResult_TonerAt10Percent_SetsStatusWarning_NotError()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(tonerPct: 10));

        // 10% is low but not critical — boundary test
        printer.Status.Should().Be(PrinterStatus.Warning);
    }

    // ── ApplyPollResult — domain events ──────────────────────────────────────

    [Fact]
    public void ApplyPollResult_LowToner_RaisesTonerLowEvent()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(tonerPct: 15));

        printer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PrinterTonerLowEvent>();
    }

    [Fact]
    public void ApplyPollResult_HealthyToner_RaisesNoEvents()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(tonerPct: 75));

        printer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ApplyPollResult_TonerLowEvent_ContainsCorrectSupplies()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(tonerPct: 8));

        var evt = printer.DomainEvents.OfType<PrinterTonerLowEvent>().Single();
        evt.IpAddress.Should().Be("10.0.0.1");
        evt.LowSupplies.Should().ContainSingle()
            .Which.Name.Should().Be("Black Toner");
    }

    // ── ApplyPollResult — model / serial / pages ──────────────────────────────

    [Fact]
    public void ApplyPollResult_UpdatesModelSerialAndTimestamp()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        var result  = new PrinterPollResult(
            "Canon iR-ADV C5550", "SN12345", 50_000, [], []);

        printer.ApplyPollResult(result);

        printer.Model.Should().Be("Canon iR-ADV C5550");
        printer.SerialNumber.Should().Be("SN12345");
        printer.TotalPagesPrinted.Should().Be(50_000);
        printer.LastPolledAt.Should().NotBeNull();
    }

    [Fact]
    public void ApplyPollResult_ResetsOfflineCounter()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.RecordSnmpUnreachable();
        printer.RecordSnmpUnreachable();
        printer.OfflineAttempts.Should().Be(2);

        printer.ApplyPollResult(PollResult(80));

        printer.OfflineAttempts.Should().Be(0);
    }

    // ── RecordSnmpUnreachable ─────────────────────────────────────────────────

    [Fact]
    public void RecordSnmpUnreachable_BelowThreshold_DoesNotGoOffline()
    {
        var printer = Printer.Create("10.0.0.1", "Test");

        printer.RecordSnmpUnreachable();
        printer.RecordSnmpUnreachable();   // threshold is 3

        printer.Status.Should().NotBe(PrinterStatus.Offline);
        printer.OfflineAttempts.Should().Be(2);
    }

    [Fact]
    public void RecordSnmpUnreachable_AtThreshold_SetsOffline()
    {
        var printer = Printer.Create("10.0.0.1", "Test");

        printer.RecordSnmpUnreachable();
        printer.RecordSnmpUnreachable();
        printer.RecordSnmpUnreachable();   // third attempt → Offline

        printer.Status.Should().Be(PrinterStatus.Offline);
    }

    // ── Rename ────────────────────────────────────────────────────────────────

    [Fact]
    public void Rename_UpdatesNameAndSetsUserOverriddenFlag()
    {
        var printer = Printer.Create("10.0.0.1", "Old Name");
        printer.Rename("New Name");

        printer.Name.Should().Be("New Name");
        printer.UserOverridden.Should().BeTrue();
    }

    [Fact]
    public void Rename_WithEmptyString_ThrowsDomainException()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        var act     = () => printer.Rename("");

        act.Should().Throw<PrinterDomainException>();
    }

    // ── Page-count history ────────────────────────────────────────────────────

    [Fact]
    public void ApplyPollResult_SecondPoll_AccumulatesPageDelta()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        var month   = DateTime.UtcNow.ToString("yyyy-MM");

        printer.ApplyPollResult(new PrinterPollResult("M", "S", 1_000, [], []));
        printer.ApplyPollResult(new PrinterPollResult("M", "S", 1_300, [], []));

        printer.PagesHistory.Should().ContainKey(month)
            .WhoseValue.Should().Be(300);
    }

    [Fact]
    public void ApplyPollResult_CounterReset_DoesNotAddNegativeDelta()
    {
        var printer = Printer.Create("10.0.0.1", "Test");

        printer.ApplyPollResult(new PrinterPollResult("M", "S", 50_000, [], []));
        printer.ApplyPollResult(new PrinterPollResult("M", "S", 100,    [], []));

        // No month entry with a negative value
        printer.PagesHistory.Should().BeEmpty();
    }

    [Fact]
    public void ApplyPollResult_FirstPoll_SetsLastTotalPagesButAddsNoDelta()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(new PrinterPollResult("M", "S", 5_000, [], []));

        // First reading just sets the baseline — no delta can be computed yet
        printer.LastTotalPages.Should().Be(5_000);
        printer.PagesHistory.Should().BeEmpty();
    }

    // ── ClearDomainEvents ─────────────────────────────────────────────────────

    [Fact]
    public void ClearDomainEvents_RemovesAllQueuedEvents()
    {
        var printer = Printer.Create("10.0.0.1", "Test");
        printer.ApplyPollResult(PollResult(5));     // raises toner-low event
        printer.DomainEvents.Should().NotBeEmpty();

        printer.ClearDomainEvents();

        printer.DomainEvents.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PrinterPollResult PollResult(int tonerPct) =>
        new("TestModel", "SN001", 10_000,
            [new Supply("Black Toner", SupplyLevel.FromRaw(tonerPct, 100), SupplyCategory.TonerCartridge)],
            []);
}
