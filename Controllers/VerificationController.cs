using EmailVerificationService.Interface;
using EmailVerificationService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EmailVerificationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VerificationController(IVerificationService verificationController) : ControllerBase
{
    private readonly IVerificationService _verificationService = verificationController;
    [AllowAnonymous]
    [HttpPost("send")]
    public async Task<IActionResult> Send(SendVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Email Adress is required" });

        var result = await _verificationService.SendVerificationEmailAsync(request);
        return result.Succeeded
            ? Ok(result)
            : StatusCode(500, result);
    }
    [AllowAnonymous]
    [HttpPost("verify")]
    public IActionResult Verify(VerifyVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Inavlid or expired Code" });

        var result = _verificationService.VerifyVerificationCode(request);
        return result.Succeeded
            ? Ok(result)
            : StatusCode(500, result);

    }
}
