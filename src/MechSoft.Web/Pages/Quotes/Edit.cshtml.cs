using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Common;
using MechSoft.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Quotes;

public class EditModel : PageModel
{
    private readonly IQuoteService _service;
    private readonly IPdfDocumentService _pdfService;

    public EditModel(IQuoteService service, IPdfDocumentService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    [BindProperty]
    public QuoteInput Form { get; set; } = new();

    public bool IsNew => string.IsNullOrWhiteSpace(Form.Id);

    public async Task<IActionResult> OnGetPdfAsync(string id, CancellationToken ct)
    {
        var pdf = await _pdfService.GenerateQuotePdfAsync(id, ct);
        if (pdf is null) return NotFound();
        return File(pdf, "application/pdf", $"{id}.pdf");
    }

    public async Task<IActionResult> OnGetAsync(string? id, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var existing = await _service.GetByIdAsync(id, ct);
            if (existing is null) return RedirectToPage("Index");
            Form = QuoteInput.FromDto(existing);
        }
        else
        {
            var blank = _service.CreateBlank();
            Form = QuoteInput.FromDto(blank);
            Form.Lines.Add(new QuoteLineInput());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var lines = Form.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.PartNo) || !string.IsNullOrWhiteSpace(l.Description))
            .Select(l => new QuoteLineDto(l.PartNo, l.Description, l.GstPercent, l.Qty, l.Unit, l.Rate))
            .ToList();

        var dto = new QuoteSaveDto(
            string.IsNullOrWhiteSpace(Form.Id) ? null : Form.Id,
            Form.CustomerCompany, Form.CustomerName, Form.CustomerAddress, Form.CustomerEmail,
            Form.QuoteDate, Form.ValidUntil, Form.Status, Form.Gstin, Form.Pan, Form.Signatory, Form.Notes,
            Form.CompanyName, Form.CompanyAddress, Form.CompanyMobile, Form.CompanyEmail, Form.CompanyWebsite,
            lines);

        var savedId = await _service.SaveAsync(dto, ct);
        return RedirectToPage("Edit", new { id = savedId });
    }

    public async Task<IActionResult> OnPostConvertAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Form.Id)) return RedirectToPage("Index");
        var invoiceId = await _service.ConvertToInvoiceAsync(Form.Id, ct);
        return RedirectToPage("/Billing/Edit", new { id = invoiceId });
    }

    public class QuoteInput
    {
        public string? Id { get; set; }
        public string CustomerCompany { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime QuoteDate { get; set; } = DateTime.Today;
        public DateTime? ValidUntil { get; set; }
        public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
        public string Gstin { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string Signatory { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CompanyName { get; set; } = LetterheadInfo.CompanyName;
        public string CompanyAddress { get; set; } = LetterheadInfo.CompanyAddress;
        public string CompanyMobile { get; set; } = LetterheadInfo.CompanyMobile;
        public string CompanyEmail { get; set; } = LetterheadInfo.CompanyEmail;
        public string CompanyWebsite { get; set; } = LetterheadInfo.CompanyWebsite;
        public string? ConvertedInvoiceId { get; set; }
        public List<QuoteLineInput> Lines { get; set; } = new();

        public static QuoteInput FromDto(QuoteDetailDto dto) => new()
        {
            Id = string.IsNullOrWhiteSpace(dto.Id) ? null : dto.Id,
            CustomerCompany = dto.CustomerCompany,
            CustomerName = dto.CustomerName,
            CustomerAddress = dto.CustomerAddress,
            CustomerEmail = dto.CustomerEmail,
            QuoteDate = dto.QuoteDate,
            ValidUntil = dto.ValidUntil,
            Status = dto.Status,
            Gstin = dto.Gstin,
            Pan = dto.Pan,
            Signatory = dto.Signatory,
            Notes = dto.Notes,
            CompanyName = dto.CompanyName,
            CompanyAddress = dto.CompanyAddress,
            CompanyMobile = dto.CompanyMobile,
            CompanyEmail = dto.CompanyEmail,
            CompanyWebsite = dto.CompanyWebsite,
            ConvertedInvoiceId = dto.ConvertedInvoiceId,
            Lines = dto.Lines.Select(l => new QuoteLineInput
            {
                PartNo = l.PartNo,
                Description = l.Description,
                GstPercent = l.GstPercent,
                Qty = l.Qty,
                Unit = l.Unit,
                Rate = l.Rate,
            }).ToList(),
        };
    }

    public class QuoteLineInput
    {
        public string PartNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal GstPercent { get; set; } = LetterheadInfo.DefaultGstPercent;
        public decimal Qty { get; set; } = 1;
        public string Unit { get; set; } = LetterheadInfo.DefaultUnit;
        public decimal Rate { get; set; }
    }
}
