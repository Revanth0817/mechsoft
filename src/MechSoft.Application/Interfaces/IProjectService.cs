using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<ProjectDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<string> SaveAsync(ProjectSaveDto dto, CancellationToken ct = default);
    Task<string> GenerateNextIdAsync(CancellationToken ct = default);

    /// <summary>Ongoing projects (not Completed), most recently started first, for the Dashboard.</summary>
    Task<IReadOnlyList<ProjectListItemDto>> GetOngoingAsync(int take, CancellationToken ct = default);

    /// <summary>Saves a progress photo, replacing any existing one. Returns false if the project does not exist.</summary>
    Task<bool> UpdatePhotoAsync(string id, string contentType, byte[] content, CancellationToken ct = default);

    Task<PhotoContentDto?> GetPhotoAsync(string id, CancellationToken ct = default);

    Task<bool> RemovePhotoAsync(string id, CancellationToken ct = default);
}
