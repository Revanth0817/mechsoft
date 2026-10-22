namespace MechSoft.Domain.Entities;

/// <summary>
/// A CAD model or drawing uploaded against a part. The bytes live in the database so
/// every user of the application sees the same shared file library.
/// </summary>
public class CadFile
{
    public int Id { get; set; }
    /// <summary>Optional link to a part; null for files uploaded straight into the file library.</summary>
    public string? PartId { get; set; }
    public Part? Part { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty; // lower-case, no dot
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Autodesk Platform Services URN once the file has been sent for cloud translation.</summary>
    public string? ApsUrn { get; set; }

    /// <summary>null (never sent), pending, success or failed.</summary>
    public string? ApsStatus { get; set; }

    public byte[] Content { get; set; } = Array.Empty<byte>();
}
