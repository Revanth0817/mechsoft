using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Billing;

public class IndexModel : PageModel
{
    private readonly IInvoiceService _service;

    public IndexModel(IInvoiceService service) => _service = service;

    public IReadOnlyList<InvoiceListItemDto> Invoices { get; private set; } = Array.Empty<InvoiceListItemDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Invoices = await _service.GetAllAsync(ct);
    }
}
