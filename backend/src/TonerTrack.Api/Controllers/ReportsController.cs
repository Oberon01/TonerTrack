using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TonerTrack.Application.Printers.Queries;

namespace TonerTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    // GET /api/reports/monthly.csv
    [HttpGet("monthly.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> AllPrintersMonthlyCsv(CancellationToken ct)
    {
        var printers = await mediator.Send(new GetAllPrintersQuery(), ct);
        var sb = new StringBuilder("ip,name,month,pages\n");

        foreach (var p in printers)
        {
            var safeName = $"\"{p.Name.Replace("\"", "\"\"")}\"";
            foreach (var (month, pages) in p.MonthlyPages.OrderBy(kv => kv.Key))
                sb.Append($"{p.IpAddress},{safeName},{month},{pages}\n");
        }

        return File(
            Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv",
            $"tonertrack_monthly_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // GET /api/reports/printers/{ip}/usage.csv
    [HttpGet("printers/{ip}/usage.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> PrinterUsageCsv(string ip, CancellationToken ct)
    {
        var usage = await mediator.Send(new GetPrinterUsageQuery(ip), ct);
        var sb    = new StringBuilder();
        sb.AppendLine($"Printer,\"{usage.Name.Replace("\"", "\"\"")}\",{ip}");
        sb.AppendLine();
        sb.AppendLine("month,pages");

        foreach (var (month, pages) in usage.FullHistory.OrderBy(kv => kv.Key))
            sb.AppendLine($"{month},{pages}");

        return File(
            Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv",
            $"usage_{ip.Replace('.', '_')}.csv");
    }
}
