using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;

    public CustomerService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.CompanyName)
            .Select(c => new CustomerDto(c.Id, c.CompanyName))
            .ToListAsync(ct);
    }
}
