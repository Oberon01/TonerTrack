using FluentAssertions;
using Moq;
using TonerTrack.Application.Printers.Commands;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Repositories;
using Xunit;

namespace TonerTrack.Application.Tests;

public sealed class ImportPrintersCommandTests
{
    private readonly Mock<IPrinterRepository> _repo = new();

    [Fact]
    public async Task Handle_NewPrinters_ImportsAll()
    {
        _repo.Setup(r => r.ExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);

        var command = new ImportPrintersCommand([
            new ImportPrinterRequest("10.0.0.1", "Printer A"),
            new ImportPrinterRequest("10.0.0.2", "Printer B"),
        ]);

        var result = await new ImportPrintersHandler(_repo.Object)
            .Handle(command, default);

        result.Imported.Should().Be(2);
        result.Skipped.Should().Be(0);

        _repo.Verify(r => r.AddAsync(It.IsAny<Printer>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_DuplicateIp_SkipsExisting()
    {
        _repo.Setup(r => r.ExistsAsync("10.0.0.1", default)).ReturnsAsync(true);
        _repo.Setup(r => r.ExistsAsync("10.0.0.2", default)).ReturnsAsync(false);
        _repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);

        var command = new ImportPrintersCommand([
            new ImportPrinterRequest("10.0.0.1", "Already Exists"),
            new ImportPrinterRequest("10.0.0.2", "New Printer"),
        ]);

        var result = await new ImportPrintersHandler(_repo.Object)
            .Handle(command, default);

        result.Imported.Should().Be(1);
        result.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task Handle_BlankIpOrName_SkipsEntry()
    {
        _repo.Setup(r => r.ExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);

        var command = new ImportPrintersCommand([
            new ImportPrinterRequest("", "No IP"),
            new ImportPrinterRequest("10.0.0.1", ""),
            new ImportPrinterRequest("10.0.0.2", "Valid"),
        ]);

        var result = await new ImportPrintersHandler(_repo.Object)
            .Handle(command, default);

        result.Imported.Should().Be(1);
        _repo.Verify(r => r.AddAsync(It.IsAny<Printer>(), default), Times.Once);
    }
}
