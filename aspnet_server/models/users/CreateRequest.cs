using System.ComponentModel.DataAnnotations;
using entities;

namespace models.users;

public class CreateRequest
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(6)]
    public string? Password { get; set; }

    [Required]
    [EnumDataType(typeof(SubscriptionPlan))]
    public string? SubscriptionPlan { get; set; }
}