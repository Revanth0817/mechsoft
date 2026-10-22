using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

public interface ICadFileService
{
    /// <summary>True when a file with this extension (lower-case, no dot) may be stored. Any CAD type is accepted; only executables/scripts are refused.</summary>
    bool IsAllowed(string extension);

    /// <summary>Largest accepted file, in bytes.</summary>
    long MaxFileSizeBytes { get; }

    /// <summary>Every file in the library, optionally filtered by a name search, ordered by folder path then name.</summary>
    Task<IReadOnlyList<CadFileDto>> ListAllAsync(string? search, CancellationToken ct = default);

    Task<IReadOnlyList<CadFileDto>> ListForPartAsync(string partId, CancellationToken ct = default);

    /// <summary>Saves a file to the library (optionally linked to a part). Returns the new file id, or null if a given part does not exist.</summary>
    Task<int?> AddAsync(string? partId, string fileName, Stream content, CancellationToken ct = default);

    Task<CadFileContentDto?> GetContentAsync(int id, CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
