using EmailVerificationService.Models;

namespace EmailVerificationService.Interface;

public interface IVerificationService
{
    Task<VerificationServiceResult> SendVerificationEmailAsync(SendVerificationCodeRequest request);
    void SaveVerificationCode(SaveVerificationCodeRequest request);
    VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request);
}
