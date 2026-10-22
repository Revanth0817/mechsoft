using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.PurchaseOrders;

public class EditModel : PageModel
{
    private readonly IPurchaseOrderService _service;
    private readonly IPdfDocumentService _pdfService;

    public EditModel(IPurchaseOrderService service, IPdfDocumentService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    [BindProperty]
    public PoInput Form { get; set; } = new();

    public bool IsNew => string.IsNullOrWhiteSpace(Form.Id);

    public async Task<IActionResult> OnGetPdfAsync(string id, CancellationToken ct)
    {
        var pdf = await _pdfService.GeneratePurchaseOrderPdfAsync(id, ct);
        if (pdf is null) return NotFound();
        return File(pdf, "application/pdf", $"{id}.pdf");
    }

    public async Task<IActionResult> OnGetAsync(string? id, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var existing = await _service.GetByIdAsync(id, ct);
            if (existing is null) return RedirectToPage("Index");
            Form = PoInput.FromDto(existing);
        }
        else
        {
            var blank = _service.CreateBlank();
            Form = PoInput.FromDto(blank);
            Form.Lines.Add(new PoLineInput());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var lines = Form.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.PartNo) || !string.IsNullOrWhiteSpace(l.Description))
            .Select(l => new PurchaseOrderLineDto(l.PartNo, l.Description, l.GstPercent, l.Qty, l.Unit, l.Rate))
            .ToList();

        var dto = new PurchaseOrderSaveDto(
            string.IsNullOrWhiteSpace(Form.Id) ? null : Form.Id,
            Form.VendorCompany, Form.VendorName, Form.VendorAddress, Form.VendorEmail,
            Form.ShipCompany, Form.ShipName, Form.ShipAddress, Form.ShipMobile,
            Form.PoDate, Form.QuotationRef, Form.ShippingMethod, Form.Gstin, Form.Pan, Form.Signatory, Form.Notes,
            Form.CompanyName, Form.CompanyAddress, Form.CompanyMobile, Form.CompanyEmail, Form.CompanyWebsite,
            lines);

        var savedId = await _service.SaveAsync(dto, ct);
        return RedirectToPage("Edit", new { id = savedId });
    }

    public class PoInput
    {
        public string? Id { get; set; }
        public string VendorCompany { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string VendorAddress { get; set; } = string.Empty;
        public string VendorEmail { get; set; } = string.Empty;
        public string ShipCompany { get; set; } = string.Empty;
        public string ShipName { get; set; } = string.Empty;
        public string ShipAddress { get; set; } = string.Empty;
        public string ShipMobile { get; set; } = string.Empty;
        public DateTime PoDate { get; set; } = DateTime.Today;
        public string QuotationRef { get; set; } = string.Empty;
        public string ShippingMethod { get; set; } = "Road";
        public string Gstin { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string Signatory { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CompanyName { get; set; } = LetterheadInfo.CompanyName;
        public string CompanyAddress { get; set; } = LetterheadInfo.CompanyAddress;
        public string CompanyMobile { get; set; } = LetterheadInfo.CompanyMobile;
        public string CompanyEmail { get; set; } = LetterheadInfo.CompanyEmail;
        public string CompanyWebsite { get; set; } = LetterheadInfo.CompanyWebsite;
        public List<PoLineInput> Lines { get; set; } = new();

        public static PoInput FromDto(PurchaseOrderDetailDto dto) => new()
        {
            Id = string.IsNullOrWhiteSpace(dto.Id) ? null : dto.Id,
            VendorCompany = dto.VendorCompany,
            VendorName = dto.VendorName,
            VendorAddress = dto.VendorAddress,
            VendorEmail = dto.VendorEmail,
            ShipCompany = dto.ShipCompany,
            ShipName = dto.ShipName,
            ShipAddress = dto.ShipAddress,
            ShipMobile = dto.ShipMobile,
            PoDate = dto.PoDate,
            QuotationRef = dto.QuotationRef,
            ShippingMethod = dto.ShippingMethod,
            Gstin = dto.Gstin,
            Pan = dto.Pan,
            Signatory = dto.Signatory,
            Notes = dto.Notes,
            CompanyName = dto.CompanyName,
            CompanyAddress = dto.CompanyAddress,
            CompanyMobile = dto.CompanyMobile,
            CompanyEmail = dto.CompanyEmail,
            CompanyWebsite = dto.CompanyWebsite,
            Lines = dto.Lines.Select(l => new PoLineInput
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

    public class PoLineInput
    {
        public string PartNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal GstPercent { get; set; } = LetterheadInfo.DefaultGstPercent;
        public decimal Qty { get; set; } = 1;
        public string Unit { get; set; } = LetterheadInfo.DefaultUnit;
        public decimal Rate { get; set; }
    }
}
