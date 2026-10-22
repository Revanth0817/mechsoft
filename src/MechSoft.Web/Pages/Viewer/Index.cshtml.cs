using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Viewer;

[RequestSizeLimit(MaxUploadBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
public class IndexModel : PageModel
{
    private const long MaxUploadBytes = 524_288_000; // whole request (a folder of files); each file is capped at CadFileService.MaxFileSizeBytes (50 MB)

    private readonly ICadFileService _cadFiles;
    private readonly IApsService _aps;

    // Formats the built-in viewer renders itself; everything else needs Autodesk translation to preview.
    private static readonly HashSet<string> NativeExtensions = new()
    {
        "stl", "obj", "step", "stp", "iges", "igs", "brep", "brp", "ply", "glb", "gltf", "fbx", "dae", "3ds", "3mf", "pdf",
    };

    public IndexModel(ICadFileService cadFiles, IApsService aps)
    {
        _cadFiles = cadFiles;
        _aps = aps;
    }

    public bool ApsConfigured => _aps.IsConfigured;
    public static bool IsNative(string ext) => NativeExtensions.Contains(ext);

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    /// <summary>Id of a saved file to open in the viewer as soon as the page loads.</summary>
    [BindProperty(SupportsGet = true)]
    public int? OpenFile { get; set; }

    public IReadOnlyList<CadFileDto> Files { get; private set; } = Array.Empty<CadFileDto>();

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Files = await _cadFiles.ListAllAsync(Search, ct);
    }

    public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> uploads, CancellationToken ct)
    {
        if (uploads.Count == 0)
        {
            Error = "Choose a file or folder to upload.";
            return RedirectToPage("Index");
        }

        var saved = new List<int>();
        var skipped = new List<string>();
        var apsFailures = 0;

        foreach (var upload in uploads)
        {
            // A folder upload sends each file's relative path (e.g. "Bracket/rev2/part.sldprt") as its name.
            var name = CleanRelativePath(upload.FileName);
            var ext = Path.GetExtension(name).TrimStart('.').ToLowerInvariant();

            if (upload.Length == 0 || string.IsNullOrEmpty(name)) { skipped.Add($"{name} (empty)"); continue; }
            if (!_cadFiles.IsAllowed(ext)) { skipped.Add($"{name} (executables and scripts cannot be stored)"); continue; }
            if (upload.Length > _cadFiles.MaxFileSizeBytes)
            {
                skipped.Add($"{name} (over {_cadFiles.MaxFileSizeBytes / 1024 / 1024} MB)");
                continue;
            }

            await using var stream = upload.OpenReadStream();
            var id = await _cadFiles.AddAsync(null, name, stream, ct);
            if (id is null) continue;
            saved.Add(id.Value);

            // Formats the browser cannot read are sent to Autodesk for translation when APS is configured.
            if (!IsNative(ext) && _aps.IsConfigured)
            {
                try { await _aps.TranslateAsync(id.Value, ct); }
                catch { apsFailures++; }
            }
        }

        if (saved.Count > 0)
        {
            Message = saved.Count == 1
                ? "Saved 1 file to the library - visible to everyone using the application."
                : $"Saved {saved.Count} files to the library - visible to everyone using the application.";
            if (apsFailures > 0) Error = $"{apsFailures} file(s) could not be sent to Autodesk for preview.";
        }
        if (skipped.Count > 0)
        {
            Error = (Error is null ? "" : Error + " ") + "Skipped: " + string.Join("; ", skipped.Take(5)) + (skipped.Count > 5 ? $"; and {skipped.Count - 5} more" : "") + ".";
        }

        return saved.Count == 1
            ? RedirectToPage("Index", new { openFile = saved[0] })
            : RedirectToPage("Index");
    }

    // Normalises a browser-supplied name to a safe relative path: forward slashes, no drive, no "..".
    private static string CleanRelativePath(string raw)
    {
        var parts = raw.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p != "." && p != ".." && !p.Contains(':'))
            .ToArray();
        var path = string.Join('/', parts);
        return path.Length > 250 ? path[^250..] : path;
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id, bool inline, CancellationToken ct)
    {
        var file = await _cadFiles.GetContentAsync(id, ct);
        if (file is null) return NotFound();
        // Only PDFs are ever shown inline; every other type is sent as a download.
        if (inline && file.Extension == "pdf") return File(file.Content, "application/pdf");
        return File(file.Content, "application/octet-stream", Path.GetFileName(file.FileName));
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        if (await _cadFiles.DeleteAsync(id, ct)) Message = "File deleted.";
        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostTranslateAsync(int id, CancellationToken ct)
    {
        try
        {
            await _aps.TranslateAsync(id, ct);
            Message = "Sent to Autodesk for preview; it can take a minute.";
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        return RedirectToPage("Index", new { openFile = id });
    }

    public async Task<IActionResult> OnGetApsStatusAsync(int id, CancellationToken ct)
    {
        try
        {
            var status = await _aps.RefreshStatusAsync(id, ct);
            return new JsonResult(new { status });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { status = "error", message = ex.Message });
        }
    }

    public async Task<IActionResult> OnGetApsTokenAsync(CancellationToken ct)
    {
        if (!_aps.IsConfigured) return StatusCode(503);
        return new JsonResult(await _aps.GetViewerTokenAsync(ct));
    }
}
