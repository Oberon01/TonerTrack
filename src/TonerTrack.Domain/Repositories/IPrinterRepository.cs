using TonerTrack.Domain.Entities;

namespace TonerTrack.Domain.Repositories;

public interface IPrinterRepository
{
    Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default);
    Task<Printer?> GetByIpAsync(string ipAddress, CancellationToken ct = default);
    Task AddAsync(Printer printer, CancellationToken ct = default);
    Task UpdateAsync(Printer printer, CancellationToken ct = default);
    Task DeleteAsync(string ipAddress, CancellationToken ct = default);
    Task<bool> ExistsAsync(string ipAddress, CancellationToken ct = default);

    // @TODO: Add 'GetByNameAsync' method for lookup by printer name (case-insensitive)
    // @TODO: Add 'GetByLocationAsync' method for lookup by location (case-insensitive, partial match)
    // @TODO: Add 'GetByModelAsync' method for lookup by model (case-insensitive, partial match)
    // @TODO: Add 'GetByStatusAsync' method for lookup by printer status
    // @TODO: Add 'GetByTonerTypeAsync' method for lookup by toner type (case-insensitive, partial match)
    // @TODO: Add 'GetByModelAsync' method for lookup by model (case-insensitive, partial match)
    // @TODO: Add 'FindAsync' method that accepts a filter object for flexible querying (e.g. by location, model, status, toner type, etc.)
}
