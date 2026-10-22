using System.ComponentModel.DataAnnotations;

namespace MechSoft.Domain.Enums;

public enum StockStatus
{
    [Display(Name = "In Stock")]
    InStock = 0,

    [Display(Name = "Low Stock")]
    LowStock = 1,

    [Display(Name = "Out of Stock")]
    OutOfStock = 2
}
