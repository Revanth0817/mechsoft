using MechSoft.Domain.Enums;

namespace MechSoft.Domain.Entities;

/// <summary>
/// A machine-manufacturing job: a specific machine built for a customer, tracked from
/// planning through completion. Shown on the Dashboard as "Project Details".
/// </summary>
public class Project
{
    public string Id { get; set; } = string.Empty; // e.g. "PRJ-001"
    public string Name { get; set; } = string.Empty;
    public string MachineType { get; set; } = string.Empty; // e.g. "CNC Vertical Machining Center"
    public string CustomerName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime? TargetDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    /// <summary>0-100. Manually tracked; not derived, since build stages vary per machine.</summary>
    public int CompletionPercent { get; set; }

    public string? Notes { get; set; }

    // Progress photo, stored in the database so it is shared by everyone using the app.
    public byte[]? Photo { get; set; }
    public string? PhotoContentType { get; set; }
}
