using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public sealed class LoginViewModel
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required, StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Participant";

    [Phone, StringLength(25)]
    public string? PhoneNumber { get; set; }
}

public sealed class ProfileViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required, StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Phone, StringLength(25)]
    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? ProfileImageUrl { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? ProfileImage { get; set; }
}
