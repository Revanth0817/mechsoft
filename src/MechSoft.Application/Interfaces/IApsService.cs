using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

/// <summary>Autodesk Platform Services: cloud translation and viewing of proprietary CAD formats.</summary>
public interface IApsService
{
    /// <summary>False until Aps:ClientId and Aps:ClientSecret are configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Uploads a stored file to Autodesk and starts translation. Returns the resulting status.</summary>
    Task<string> TranslateAsync(int cadFileId, CancellationToken ct = default);

    /// <summary>Checks translation progress and saves it. Returns pending / success / failed, or null if never sent.</summary>
    Task<string?> RefreshStatusAsync(int cadFileId, CancellationToken ct = default);

    /// <summary>Short-lived, read-only token for the browser-side APS Viewer.</summary>
    Task<ApsTokenDto> GetViewerTokenAsync(CancellationToken ct = default);
}
