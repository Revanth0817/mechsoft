using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Quotes;

public class IndexModel : PageModel
{
    private readonly IQuoteService _service;

    public IndexModel(IQuoteService service) => _service = service;

    public IReadOnlyList<QuoteListItemDto> Quotes { get; private set; } = Array.Empty<QuoteListItemDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Quotes = await _service.GetAllAsync(ct);
    }

    public async Task<IActionResult> OnPostConvertAsync(string id, CancellationToken ct)
    {
        var invoiceId = await _service.ConvertToInvoiceAsync(id, ct);
        return RedirectToPage("/Billing/Edit", new { id = invoiceId });
    }
}
