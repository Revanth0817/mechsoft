using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.DeliveryChallans;

public class EditModel : PageModel
{
    private readonly IDeliveryChallanService _service;
    private readonly IPartService _partService;
    private readonly IPdfDocumentService _pdfService;

    public EditModel(IDeliveryChallanService service, IPartService partService, IPdfDocumentService pdfService)
    {
        _service = service;
        _partService = partService;
        _pdfService = pdfService;
    }

    [BindProperty]
    public DcInput Form { get; set; } = new();

    public bool IsNew => string.IsNullOrWhiteSpace(Form.Id);

    public IReadOnlyList<PartListItemDto> AllParts { get; private set; } = Array.Empty<PartListItemDto>();

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetPdfAsync(string id, CancellationToken ct)
    {
        var pdf = await _pdfService.GenerateDeliveryChallanPdfAsync(id, ct);
        if (pdf is null) return NotFound();
        return File(pdf, "application/pdf", $"{id}.pdf");
    }

    public async Task<IActionResult> OnGetAsync(string? id, CancellationToken ct)
    {
        AllParts = await _partService.GetAllAsync(null, ct);

        if (!string.IsNullOrWhiteSpace(id))
        {
            var existing = await _service.GetByIdAsync(id, ct);
            if (existing is null) return RedirectToPage("Index");
            Form = DcInput.FromDto(existing);
        }
        else
        {
            var blank = await _service.CreateBlankAsync(ct);
            Form = DcInput.FromDto(blank);
            Form.Lines.Add(new DcLineInput());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        AllParts = await _partService.GetAllAsync(null, ct);
        var partsById = AllParts.ToDictionary(p => p.Id);

        var lineDtos = new List<DeliveryChallanLineDto>();
        foreach (var l in Form.Lines.Where(l => !string.IsNullOrWhiteSpace(l.PartId)))
        {
            partsById.TryGetValue(l.PartId, out var part);
            lineDtos.Add(new DeliveryChallanLineDto(
                l.PartId,
                part is null ? l.PartId : $"{part.Sku} — {part.Name}",
                part?.Stock ?? 0,
                l.GstPercent, l.Qty, l.Unit, l.Rate));
        }

        var dto = new DeliveryChallanSaveDto(
            string.IsNullOrWhiteSpace(Form.Id) ? null : Form.Id,
            Form.CustomerCompany, Form.CustomerName, Form.CustomerAddress, Form.CustomerMobile,
            Form.DcDate, Form.Gstin, Form.Pan, Form.Signatory, Form.Notes,
            Form.CompanyName, Form.CompanyAddress, Form.CompanyMobile, Form.CompanyEmail, Form.CompanyWebsite,
            lineDtos);

        var result = await _service.SaveAsync(dto, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, string.Join(" ", result.Errors));
            return Page();
        }

        return RedirectToPage("Edit", new { id = result.Id });
    }

    public class DcInput
    {
        public string? Id { get; set; }
        public string CustomerCompany { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public DateTime DcDate { get; set; } = DateTime.Today;
        public string Gstin { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string Signatory { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CompanyName { get; set; } = LetterheadInfo.CompanyName;
        public string CompanyAddress { get; set; } = LetterheadInfo.CompanyAddress;
        public string CompanyMobile { get; set; } = LetterheadInfo.CompanyMobile;
        public string CompanyEmail { get; set; } = LetterheadInfo.CompanyEmail;
        public string CompanyWebsite { get; set; } = LetterheadInfo.CompanyWebsite;
        public List<DcLineInput> Lines { get; set; } = new();

        public static DcInput FromDto(DeliveryChallanDetailDto dto) => new()
        {
            Id = string.IsNullOrWhiteSpace(dto.Id) ? null : dto.Id,
            CustomerCompany = dto.CustomerCompany,
            CustomerName = dto.CustomerName,
            CustomerAddress = dto.CustomerAddress,
            CustomerMobile = dto.CustomerMobile,
            DcDate = dto.DcDate,
            Gstin = dto.Gstin,
            Pan = dto.Pan,
            Signatory = dto.Signatory,
            Notes = dto.Notes,
            CompanyName = dto.CompanyName,
            CompanyAddress = dto.CompanyAddress,
            CompanyMobile = dto.CompanyMobile,
            CompanyEmail = dto.CompanyEmail,
            CompanyWebsite = dto.CompanyWebsite,
            Lines = dto.Lines.Select(l => new DcLineInput
            {
                PartId = l.PartId,
                GstPercent = l.GstPercent,
                Qty = l.Qty,
                Unit = l.Unit,
                Rate = l.Rate,
            }).ToList(),
        };
    }

    public class DcLineInput
    {
        public string PartId { get; set; } = string.Empty;
        public decimal GstPercent { get; set; } = LetterheadInfo.DefaultGstPercent;
        public decimal Qty { get; set; } = 1;
        public string Unit { get; set; } = LetterheadInfo.DefaultUnit;
        public decimal Rate { get; set; }
    }
}
