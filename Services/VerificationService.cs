using System.Diagnostics;
using System.Text.Json;
using Azure;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailVerificationService.Interface;
using EmailVerificationService.Models;
using Microsoft.Extensions.Caching.Memory;

namespace EmailVerificationService.Services;


public class VerificationService : IVerificationService
{
    private readonly IConfiguration _configuration;
    private readonly EmailClient _emailClient;
    private readonly IMemoryCache _cache;
    private readonly ServiceBusClient _serviceBusClient;
    private static readonly Random _random = new();

    public VerificationService(
        IConfiguration configuration,
        EmailClient emailClient,
        IMemoryCache cache,
        ServiceBusClient serviceBusClient)
    {
        _configuration = configuration;
        _emailClient = emailClient;
        _cache = cache;
        _serviceBusClient = serviceBusClient;
    }

    public async Task<VerificationServiceResult> SendVerificationEmailAsync(SendVerificationCodeRequest request)
    {
        try
        {

            if (request == null || string.IsNullOrEmpty(request.Email))
                return new VerificationServiceResult { Succeeded = false, Message = "Invalid request." };
            var verCode = _random.Next(100000, 999999).ToString();
            var subject = $"Your Email Verification Code: {verCode}";
            var plainTextContent = @$"
            Hello,

            Thank you for registering to Ventrixe!

            To verify your email address, please use the following verification code:

            Verification Code: {verCode}

            Alternatively, you can click the link below to verify your email directly:
            // Replace with the full verification URL including token or user ID
            https://yourdomain.com/verify-email?code={verCode}&email=your@email.com

            This code will expire in 15 minutes.

            If you did not create an account or request verification, please ignore this email.

            Best regards
            ";
            var htmlContent = @$"
                    <!DOCTYPE html>
                    <html lang='en'>
                    <head>
                      <meta charset='UTF-8'>
                      <meta name=''viewport' content='width=device-width, initial-scale=1.0'>
                      <title>Email Verification</title>
                    </head>
                    <body style='font-family: Arial, sans-serif; background-color: #f9f9f9; padding: 20px;'>
                      <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; padding: 30px; box-shadow: 0 0 10px rgba(0,0,0,0.05);'>
                        <h2 style='color: #37437D;''>Verify your email</h2>
                        <p style='color: #37437D;'>
                          Hello,<br><br>
                          Thank you for registering to <strong>Ventrixe</strong>!
                        </p>
                        <p style='color: #37437D;'>
                          To verify your email address, please use the following verification code:
                        </p>
                        <p style='font-size: 20px; font-weight: bold; color: #37437D; text-align: center;'>
                          {verCode}
                        </p>
                        <p style='color: #37437D;'>Or click the button below:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                          <a href='https://yourdomain.com/verify-email?code={verCode}&email=your@email.com' 
                             style='display: inline-block; background-color: #F26CF9; color: white; padding: 12px 24px; border-radius: 5px; text-decoration: none; font-weight: bold;'>
                            Verify Email
                          </a>
                        </div>
                        <p style='color: #37437D;'>
                          This code will expire in 15 minutes.
                        </p>
                        <p style='color: #777779; font-size: 12px;'>
                          If you did not create an account or request verification, you can safely ignore this email.
                        </p>
                        <p style='color: #37437D;'>
                          Best regards,<br>
                          The Ventrixe Team
                        </p>
                      </div>
                    </body>
                    </html>
                    ";
            var emailMessage = new EmailMessage(
                senderAddress: _configuration["ACS:SenderAdress"],
                recipients: new EmailRecipients([new(request.Email)]),
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                });
            var emailSendOp = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
            SaveVerificationCode(new SaveVerificationCodeRequest { Email = request.Email, Code = verCode, ValidFor = TimeSpan.FromMinutes(15) });

            return new VerificationServiceResult { Succeeded = true, Message = "Verification email sent successfully." };

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return new VerificationServiceResult { Succeeded = false, ErrorMessage = ex.Message };
        }

    }

    public void SaveVerificationCode(SaveVerificationCodeRequest request)
    {
        _cache.Set(request.Email.ToLowerInvariant(), request.Code, request.ValidFor);
    }

    public VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request)
    {
        var key = request.Email.ToLowerInvariant();
        if (_cache.TryGetValue(key, out string? storedCode))
        {
            if (storedCode == request.Code)
            {
                _cache.Remove(key);

                PublishEmailConfirmed(request.Email).GetAwaiter().GetResult();

                return new VerificationServiceResult { Succeeded = true, Message = "Verification Successful." };
            }
        }
        return new VerificationServiceResult { Succeeded = false, Message = "Verification Failed or Code Expired." };
    }

    private async Task PublishEmailConfirmed(string email)
    {
        var queueName = _configuration["ServiceBus:QueueName"];
        ServiceBusSender sender = _serviceBusClient.CreateSender(queueName);

        var payload = new { Email = email.ToLowerInvariant() };
        string jsonPayload = JsonSerializer.Serialize(payload);

        ServiceBusMessage message = new ServiceBusMessage(jsonPayload)
        {
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message);
    }
}
