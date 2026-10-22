using System.ComponentModel.DataAnnotations;
using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Products;

[RequestSizeLimit(MaxPhotoBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = MaxPhotoBytes)]
public class IndexModel : PageModel
{
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

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(NewPart.VendorEmail) &&
            !System.Net.Mail.MailAddress.TryCreate(NewPart.VendorEmail.Trim(), out _))
        {
            ModelState.AddModelError("NewPart.VendorEmail", "Enter a valid vendor email address.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        var specs = new List<PartSpecDto>();
        for (var i = 0; i < NewPart.SpecLabels.Count; i++)
        {
            var label = NewPart.SpecLabels[i];
            var value = i < NewPart.SpecValues.Count ? NewPart.SpecValues[i] : string.Empty;
            if (!string.IsNullOrWhiteSpace(label))
            {
                specs.Add(new PartSpecDto(label.Trim(), value?.Trim() ?? string.Empty, i));
            }
        }

        var dto = new PartSaveDto(
            string.IsNullOrWhiteSpace(NewPart.Id) ? null : NewPart.Id,
            NewPart.Sku, NewPart.Name, NewPart.Material, NewPart.Category,
            NewPart.Stock, NewPart.Status, NewPart.Notes, specs,
            NewPart.VendorName?.Trim() ?? string.Empty, NewPart.VendorContactPerson?.Trim() ?? string.Empty,
            NewPart.VendorEmail?.Trim() ?? string.Empty, NewPart.VendorPhone?.Trim() ?? string.Empty);

        var savedId = await _partService.SaveAsync(dto, ct);
        return RedirectToPage(new { selectedId = savedId });
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
            PhotoError = $"Photo is too large (max {MaxPhotoBytes / 1024 / 1024} MB).";
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

    private async Task LoadAsync(CancellationToken ct)
    {
        Parts = await _partService.GetAllAsync(Search, ct);
        var idToLoad = SelectedId ?? Parts.FirstOrDefault()?.Id;
        if (idToLoad is not null)
        {
            Selected = await _partService.GetByIdAsync(idToLoad, ct);
        }
    }

    public class PartFormInput
    {
        public string? Id { get; set; }

        [Required, StringLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Material { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        // Stock and status are managed on the Inventory page; they are carried through here unchanged.
        public StockStatus Status { get; set; } = StockStatus.OutOfStock;

        [StringLength(200)]
        public string? VendorName { get; set; }

        [StringLength(100)]
        public string? VendorContactPerson { get; set; }

        [StringLength(200)]
        public string? VendorEmail { get; set; }

        [StringLength(30)]
        public string? VendorPhone { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public List<string> SpecLabels { get; set; } = new();
        public List<string> SpecValues { get; set; } = new();
    }
}
