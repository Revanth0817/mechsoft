namespace MechSoft.Application.DTOs;

public record CadFileDto(int Id, string? PartId, string FileName, string Extension, long SizeBytes, DateTime UploadedAt,
    string? ApsUrn, string? ApsStatus);

public record CadFileContentDto(string FileName, string Extension, byte[] Content);

public record ApsTokenDto(string AccessToken, int ExpiresIn);
