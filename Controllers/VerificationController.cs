using EmailVerificationService.Interface;
using EmailVerificationService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EmailVerificationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VerificationController : ControllerBase
{
    private readonly IVerificationService _verificationService;

    public VerificationController(IVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    [AllowAnonymous]
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Email Address is required" });

        var result = await _verificationService.SendVerificationEmailAsync(request);
        return result.Succeeded
            ? Ok(result)
            : StatusCode(500, result);
    }

    [AllowAnonymous]
    [HttpPost("verify")]
    public IActionResult Verify([FromBody] VerifyVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Invalid or expired code" });

        var result = _verificationService.VerifyVerificationCode(request);
        return result.Succeeded
            ? Ok(result)
            : StatusCode(500, result);
    }
}
