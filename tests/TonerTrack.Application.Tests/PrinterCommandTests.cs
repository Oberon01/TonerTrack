using FluentAssertions;
using Moq;
using TonerTrack.Application.Printers.Commands;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Exceptions;
using TonerTrack.Domain.Repositories;
using Xunit;

namespace TonerTrack.Application.Tests;

public sealed class AddPrinterCommandTests
{
    private readonly Mock<IPrinterRepository> _repo = new();

    [Fact]
    public async Task Handle_NewIp_AddsPrinterAndReturnsDto()
    {
        _repo.Setup(r => r.ExistsAsync("10.0.0.5", default)).ReturnsAsync(false);

        var dto = await new AddPrinterHandler(_repo.Object)
            .Handle(new AddPrinterCommand("Finance Printer", "10.0.0.5", "public"), default);

        dto.IpAddress.Should().Be("10.0.0.5");
        dto.Name.Should().Be("Finance Printer");

        _repo.Verify(r => r.AddAsync(
            It.Is<Printer>(p => p.IpAddress == "10.0.0.5" && p.Name == "Finance Printer"),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateIp_ThrowsDomainException()
    {
        _repo.Setup(r => r.ExistsAsync("10.0.0.5", default)).ReturnsAsync(true);

        var act = () => new AddPrinterHandler(_repo.Object)
            .Handle(new AddPrinterCommand("Finance Printer", "10.0.0.5"), default);

        await act.Should().ThrowAsync<PrinterDomainException>()
            .WithMessage("*already exists*");
    }
}

public sealed class UpdatePrinterCommandTests
{
    private readonly Mock<IPrinterRepository> _repo = new();

    [Fact]
    public async Task Handle_ExistingPrinter_UpdatesNameAndCommunity()
    {
        var printer = Printer.Create("10.0.0.1", "Old Name", "public");
        _repo.Setup(r => r.GetByIpAsync("10.0.0.1", default)).ReturnsAsync(printer);

        var dto = await new UpdatePrinterHandler(_repo.Object)
            .Handle(new UpdatePrinterCommand("10.0.0.1", "New Name", "private"), default);

        dto.Name.Should().Be("New Name");
        dto.Community.Should().Be("private");

        _repo.Verify(r => r.UpdateAsync(printer, default), Times.Once);
    }

    [Fact]
    public async Task Handle_NullName_DoesNotChangeName()
    {
        var printer = Printer.Create("10.0.0.1", "Unchanged", "public");
        _repo.Setup(r => r.GetByIpAsync("10.0.0.1", default)).ReturnsAsync(printer);

        var dto = await new UpdatePrinterHandler(_repo.Object)
            .Handle(new UpdatePrinterCommand("10.0.0.1", null, "snmpv2"), default);

        dto.Name.Should().Be("Unchanged");
        dto.Community.Should().Be("snmpv2");
    }

    [Fact]
    public async Task Handle_PrinterNotFound_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.GetByIpAsync("99.0.0.1", default)).ReturnsAsync((Printer?)null);

        var act = () => new UpdatePrinterHandler(_repo.Object)
            .Handle(new UpdatePrinterCommand("99.0.0.1", "X", null), default);

        await act.Should().ThrowAsync<PrinterNotFoundException>();
    }
}

public sealed class DeletePrinterCommandTests
{
    private readonly Mock<IPrinterRepository> _repo = new();

    [Fact]
    public async Task Handle_ExistingPrinter_DeletesIt()
    {
        _repo.Setup(r => r.ExistsAsync("10.0.0.1", default)).ReturnsAsync(true);

        await new DeletePrinterHandler(_repo.Object)
            .Handle(new DeletePrinterCommand("10.0.0.1"), default);

        _repo.Verify(r => r.DeleteAsync("10.0.0.1", default), Times.Once);
    }

    [Fact]
    public async Task Handle_PrinterNotFound_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.ExistsAsync("99.0.0.1", default)).ReturnsAsync(false);

        var act = () => new DeletePrinterHandler(_repo.Object)
            .Handle(new DeletePrinterCommand("99.0.0.1"), default);

        await act.Should().ThrowAsync<PrinterNotFoundException>();
    }
}
