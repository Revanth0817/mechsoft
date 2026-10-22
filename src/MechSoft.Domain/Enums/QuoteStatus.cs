using System.ComponentModel.DataAnnotations;

namespace MechSoft.Domain.Enums;

public enum QuoteStatus
{
    [Display(Name = "Draft")]
    Draft = 0,

    [Display(Name = "Sent")]
    Sent = 1,

    [Display(Name = "Accepted")]
    Accepted = 2,

    [Display(Name = "Rejected")]
    Rejected = 3
}
