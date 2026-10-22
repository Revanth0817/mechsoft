using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Billing;

public class EditModel : PageModel
{
    private readonly IInvoiceService _service;
    private readonly IPdfDocumentService _pdfService;

    public EditModel(IInvoiceService service, IPdfDocumentService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    [BindProperty]
    public InvoiceInput Form { get; set; } = new();

    public bool IsNew => string.IsNullOrWhiteSpace(Form.Id);

    public async Task<IActionResult> OnGetPdfAsync(string id, CancellationToken ct)
    {
        var pdf = await _pdfService.GenerateInvoicePdfAsync(id, ct);
        if (pdf is null) return NotFound();
        return File(pdf, "application/pdf", $"{id}.pdf");
    }

    public async Task<IActionResult> OnGetAsync(string? id, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var existing = await _service.GetByIdAsync(id, ct);
            if (existing is null) return RedirectToPage("Index");
            Form = InvoiceInput.FromDto(existing);
        }
        else
        {
            var blank = _service.CreateBlank();
            Form = InvoiceInput.FromDto(blank);
            Form.Lines.Add(new InvoiceLineInput());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var lines = Form.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.PartNo) || !string.IsNullOrWhiteSpace(l.Description))
            .Select(l => new InvoiceLineDto(l.PartNo, l.Description, l.GstPercent, l.Qty, l.Unit, l.Rate))
            .ToList();

        var dto = new InvoiceSaveDto(
            string.IsNullOrWhiteSpace(Form.Id) ? null : Form.Id,
            Form.CustomerCompany, Form.CustomerName, Form.CustomerAddress, Form.CustomerEmail,
            Form.InvoiceDate, Form.DueDate, Form.DcRef, Form.PaymentTerms, Form.Notes, Form.Discount, Form.Shipping,
            Form.Gstin, Form.Pan, Form.Signatory,
            Form.CompanyName, Form.CompanyAddress, Form.CompanyMobile, Form.CompanyEmail, Form.CompanyWebsite,
            lines);

        var savedId = await _service.SaveAsync(dto, ct);
        return RedirectToPage("Edit", new { id = savedId });
    }

    public class InvoiceInput
    {
        public string? Id { get; set; }
        public string CustomerCompany { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Today;
        public DateTime? DueDate { get; set; }
        public string DcRef { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = "Net 30";
        public string Notes { get; set; } = string.Empty;
        public decimal Discount { get; set; }
        public decimal Shipping { get; set; }
        public string Gstin { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string Signatory { get; set; } = string.Empty;
        public string CompanyName { get; set; } = LetterheadInfo.CompanyName;
        public string CompanyAddress { get; set; } = LetterheadInfo.CompanyAddress;
        public string CompanyMobile { get; set; } = LetterheadInfo.CompanyMobile;
        public string CompanyEmail { get; set; } = LetterheadInfo.CompanyEmail;
        public string CompanyWebsite { get; set; } = LetterheadInfo.CompanyWebsite;
        public List<InvoiceLineInput> Lines { get; set; } = new();

        public static InvoiceInput FromDto(InvoiceDetailDto dto) => new()
        {
            Id = string.IsNullOrWhiteSpace(dto.Id) ? null : dto.Id,
            CustomerCompany = dto.CustomerCompany,
            CustomerName = dto.CustomerName,
            CustomerAddress = dto.CustomerAddress,
            CustomerEmail = dto.CustomerEmail,
            InvoiceDate = dto.InvoiceDate,
            DueDate = dto.DueDate,
            DcRef = dto.DcRef,
            PaymentTerms = dto.PaymentTerms,
            Notes = dto.Notes,
            Discount = dto.Discount,
            Shipping = dto.Shipping,
            Gstin = dto.Gstin,
            Pan = dto.Pan,
            Signatory = dto.Signatory,
            CompanyName = dto.CompanyName,
            CompanyAddress = dto.CompanyAddress,
            CompanyMobile = dto.CompanyMobile,
            CompanyEmail = dto.CompanyEmail,
            CompanyWebsite = dto.CompanyWebsite,
            Lines = dto.Lines.Select(l => new InvoiceLineInput
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

    public class InvoiceLineInput
    {
        public string PartNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal GstPercent { get; set; } = LetterheadInfo.DefaultGstPercent;
        public decimal Qty { get; set; } = 1;
        public string Unit { get; set; } = LetterheadInfo.DefaultUnit;
        public decimal Rate { get; set; }
    }
}
