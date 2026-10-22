using System.ComponentModel.DataAnnotations;
using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Projects;

[RequestSizeLimit(MaxPhotoBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = MaxPhotoBytes)]
public class IndexModel : PageModel
{
    private const long MaxPhotoBytes = 8_388_608; // 8 MB
    private static readonly HashSet<string> AllowedPhotoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };

    private readonly IProjectService _projectService;

    public IndexModel(IProjectService projectService) => _projectService = projectService;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedId { get; set; }

    public IReadOnlyList<ProjectListItemDto> Projects { get; private set; } = Array.Empty<ProjectListItemDto>();
    public ProjectDetailDto? Selected { get; private set; }

    [BindProperty]
    public ProjectFormInput Form { get; set; } = new();

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
        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        var dto = new ProjectSaveDto(
            string.IsNullOrWhiteSpace(Form.Id) ? null : Form.Id,
            Form.Name.Trim(), Form.MachineType.Trim(), Form.CustomerName.Trim(),
            Form.StartDate, Form.TargetDate, Form.Status, Form.CompletionPercent, Form.Notes?.Trim());

        var savedId = await _projectService.SaveAsync(dto, ct);
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
        await _projectService.UpdatePhotoAsync(id, photo.ContentType, ms.ToArray(), ct);

        PhotoMessage = "Photo updated.";
        return RedirectToPage(new { selectedId = id });
    }

    public async Task<IActionResult> OnPostRemovePhotoAsync(string id, CancellationToken ct)
    {
        await _projectService.RemovePhotoAsync(id, ct);
        PhotoMessage = "Photo removed.";
        return RedirectToPage(new { selectedId = id });
    }

    public async Task<IActionResult> OnGetPhotoAsync(string id, CancellationToken ct)
    {
        var photo = await _projectService.GetPhotoAsync(id, ct);
        if (photo is null) return NotFound();
        return File(photo.Content, photo.ContentType);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Projects = await _projectService.GetAllAsync(Search, ct);
        var idToLoad = SelectedId ?? Projects.FirstOrDefault()?.Id;
        if (idToLoad is not null)
        {
            Selected = await _projectService.GetByIdAsync(idToLoad, ct);
        }
    }

    public class ProjectFormInput
    {
        public string? Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string MachineType { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Today;

        public DateTime? TargetDate { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

        [Range(0, 100)]
        public int CompletionPercent { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }
}
