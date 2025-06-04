namespace EmailVerificationService.Models;

public class SaveVerificationCodeRequest
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public TimeSpan ValidFor { get; set; }
}
