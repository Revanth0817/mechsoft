using System.ComponentModel.DataAnnotations;

namespace MechSoft.Domain.Enums;

public enum ProjectStatus
{
    [Display(Name = "Planning")]
    Planning = 0,

    [Display(Name = "In Progress")]
    InProgress = 1,

    [Display(Name = "Testing")]
    Testing = 2,

    [Display(Name = "On Hold")]
    OnHold = 3,

    [Display(Name = "Completed")]
    Completed = 4
}
