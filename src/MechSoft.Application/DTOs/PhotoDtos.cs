namespace MechSoft.Application.DTOs;

/// <summary>A stored photo's bytes and content type, for streaming back to the browser.</summary>
public record PhotoContentDto(string ContentType, byte[] Content);
