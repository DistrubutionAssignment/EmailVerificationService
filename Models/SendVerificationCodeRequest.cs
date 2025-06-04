using System.ComponentModel.DataAnnotations;

namespace EmailVerificationService.Models;

public class SendVerificationCodeRequest
{
    [Required]
    public string Email { get; set; } = null!;

}
