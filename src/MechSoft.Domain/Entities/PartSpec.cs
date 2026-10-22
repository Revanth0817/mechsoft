namespace MechSoft.Domain.Entities;

/// <summary>
/// A single free-form label/value specification row belonging to a Part
/// (replicates the `specs: {label: value}` dictionary in the original React app).
/// </summary>
public class PartSpec
{
    public int Id { get; set; }
    public string PartId { get; set; } = string.Empty;
    public Part? Part { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
