using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Products;

public class IndexModel : PageModel
{
    // REQ-05: the 8 MB photo limit applies to the uploaded file only (checked below), so this page
    // deliberately carries no [RequestSizeLimit] / [RequestFormLimits] - those would cap the whole
    // multipart request (form fields + boundary overhead too) and refuse it with a bare HTTP 413
    // before this handler ever runs, which is not wanted.
    private const long MaxPhotoBytes = 8_388_608; // 8 MB
    private static readonly HashSet<string> AllowedPhotoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };

    private readonly IPartService _partService;

    public IndexModel(IPartService partService) => _partService = partService;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedId { get; set; }

    public IReadOnlyList<PartListItemDto> Parts { get; private set; } = Array.Empty<PartListItemDto>();
    public PartDetailDto? Selected { get; private set; }

    [BindProperty]
    public PartFormInput NewPart { get; set; } = new();

    [TempData]
    public string? PhotoMessage { get; set; }

    [TempData]
    public string? PhotoError { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    /// <summary>
    /// REQ-04: creates a part from its editable fields only. No Id, Stock, Status or Version is read
    /// from the form - the server assigns the next P-number, Stock 0, Out of Stock and Version 1.
    /// </summary>
    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        var result = await _partService.CreateAsync(BuildDto(), ct);

        if (result.Outcome == PartSaveOutcome.ValidationFailed)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(error.Field, error.Message);
            await LoadAsync(ct);
            return Page();
        }

        return RedirectToPage(new { selectedId = result.Id });
    }

    /// <summary>
    /// REQ-04: the part identity and the expected version come from the URL (set by the Edit link,
    /// never from a form field). REQ-02: a stale version is rejected, nothing is saved.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateAsync([FromQuery] string id, [FromQuery] int version, CancellationToken ct)
    {
        var result = await _partService.UpdateAsync(id, version, BuildDto(), ct);

        switch (result.Outcome)
        {
            case PartSaveOutcome.ValidationFailed:
                foreach (var error in result.Errors) ModelState.AddModelError(error.Field, error.Message);
                SelectedId = id;
                await LoadAsync(ct);
                return Page();

            case PartSaveOutcome.NotFound:
                ModelState.AddModelError(string.Empty, "Part not found.");
                SelectedId = id;
                await LoadAsync(ct);
                return Page();

            case PartSaveOutcome.Conflict:
                ModelState.AddModelError(string.Empty,
                    "This part was changed by someone else after you opened it. Nothing was saved. Reload the page to see the latest values.");
                SelectedId = id;
                await LoadAsync(ct);
                return Page();

            default:
                return RedirectToPage(new { selectedId = result.Id });
        }
    }

    public async Task<IActionResult> OnPostUploadPhotoAsync(string id, IFormFile? photo, CancellationToken ct)
    {
        if (photo is null || photo.Length == 0)
        {
            PhotoError = "Choose a photo to upload.";
            return RedirectToPage(new { selectedId = id });
        }
        if (!AllowedPhotoTypes.Contains(photo.ContentType))
        {
            PhotoError = "Photos must be JPEG, PNG, WEBP or GIF.";
            return RedirectToPage(new { selectedId = id });
        }
        if (photo.Length > MaxPhotoBytes)
        {
            PhotoError = "Photo is too large (max 8 MB).";
            return RedirectToPage(new { selectedId = id });
        }

        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms, ct);
        await _partService.UpdatePhotoAsync(id, photo.ContentType, ms.ToArray(), ct);

        PhotoMessage = "Photo updated.";
        return RedirectToPage(new { selectedId = id });
    }

    public async Task<IActionResult> OnPostRemovePhotoAsync(string id, CancellationToken ct)
    {
        await _partService.RemovePhotoAsync(id, ct);
        PhotoMessage = "Photo removed.";
        return RedirectToPage(new { selectedId = id });
    }

    public async Task<IActionResult> OnGetPhotoAsync(string id, CancellationToken ct)
    {
        var photo = await _partService.GetPhotoAsync(id, ct);
        if (photo is null) return NotFound();
        return File(photo.Content, photo.ContentType);
    }

    private PartSaveDto BuildDto()
    {
        // Trimming, required/length checks, email validation and dropping blank-label spec rows all
        // happen centrally in PartService (REQ-07), so the raw posted values are passed through as-is -
        // a direct request gets exactly the same result as the page (no client-side-only trimming).
        var specs = new List<PartSpecDto>();
        for (var i = 0; i < NewPart.SpecLabels.Count; i++)
        {
            var label = NewPart.SpecLabels[i];
            var value = i < NewPart.SpecValues.Count ? NewPart.SpecValues[i] : string.Empty;
            specs.Add(new PartSpecDto(label ?? string.Empty, value ?? string.Empty, i));
        }

        return new PartSaveDto(
            NewPart.Sku, NewPart.Name, NewPart.Material, NewPart.Category, NewPart.Notes, specs,
            NewPart.VendorName ?? string.Empty, NewPart.VendorContactPerson ?? string.Empty,
            NewPart.VendorEmail ?? string.Empty, NewPart.VendorPhone ?? string.Empty);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Parts = await _partService.GetAllAsync(Search, ct);
        var idToLoad = SelectedId ?? Parts.FirstOrDefault()?.Id;
        if (idToLoad is not null)
        {
            Selected = await _partService.GetByIdAsync(idToLoad, ct);
        }
    }

    /// <summary>
    /// The editable fields only (REQ-04): no Id, Stock, Status or Version property exists here, so
    /// none can ever be posted to create a part or change its stock.
    /// </summary>
    public class PartFormInput
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public string? VendorContactPerson { get; set; }
        public string? VendorEmail { get; set; }
        public string? VendorPhone { get; set; }
        public string? Notes { get; set; }
        public List<string> SpecLabels { get; set; } = new();
        public List<string> SpecValues { get; set; } = new();
    }
}
