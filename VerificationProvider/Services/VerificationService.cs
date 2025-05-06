using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Data.Contexts;
using VerificationProvider.Models;


namespace VerificationProvider.Services;

public class VerificationService(ILogger<VerificationService> logger, IServiceProvider serviceProvider) : IVerificationService
{
    private readonly ILogger<VerificationService> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public string GenerateServiceBusEmailRequest(EmailRequest emailRequest)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(emailRequest);
            if (!string.IsNullOrEmpty(payload))
            {
                return payload;

            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.GenerateServiceBusEmailRequest() :: {ex.Message}");

        }
        return null!;

    }

    public EmailRequest GenerateEmailRequest(VerificationRequest verificationRequest, string code)
    {
        try
        {
            if (!string.IsNullOrEmpty(verificationRequest.Email) && !string.IsNullOrEmpty(code))
            {
                var emailRequest = new EmailRequest()
                {
                    To = verificationRequest.Email,
                    Subject = $"Verification Code {code}",
                    HtmlBody = $@"
                    <html lang='en'>
                        <head>
                            <meta charset=""UTF-8"">
                            <meta name='viewport' content='width=device-width, initial scale=1.0'>
                            <title>VerificationCode</title>
                         </head>
                    <body>
                    <div style='color:#191919; max-width:500px'>
                        <div style='background-color: #008000; color:white; text-align:center;padding: 20px 0;'>
                        <h1 style='font-weight: 400;'>Verification Code</h1>
                        </div>
                        <div style='background-color:#d8ffb1; padding: 1rem 2rem;'>
                            <p>Dear user,</p>
                            <p>We received a request to sign in to your account:{verificationRequest.Email}.To complete the verification, please use the following code (valid for 2 minutes):</p>
                             <p class='code' style='font-weight:700; text-align: center; font-size: 48px; letter-spacing: 8px;'>
                            {code}
                             </p>
                            <div  style='color:#191919; font-size:10px;'>
                                <p>If you didn't request this code, it is possible that someone else is triyng to acces your Eden Account<span style='color:#008000;'>{verificationRequest.Email}</span>.You can't reply to this email, for more information visit Eden's Help Center.</p>
                            </div>
                        </div>
                        <div style='color:#191919; text-align: center; font-size: 10px;'>
   
                        <p>@Eden, Klovergatan 3, SE-123 45 Helsingborg, Sweden</p>
                        </div>
                    </div>
                    </body>
                    </html>
                    ",
                    PlainText = $"Please verify your account using this verification code:{code}.If you didn't request this code, it is possible that someone else is triyng to acces your Eden Account {verificationRequest.Email}.You can't reply to this email, for more information visit our Help Center."


                };
                return emailRequest;
            }

        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR :GenerateVerificationCode.GenerateEmailRequest :: {ex.Message}");

        }
        return null!;
    }

    public VerificationRequest UnpackVerificationRequest(ServiceBusReceivedMessage message)
    {
        try
        {
            var verificationRequest = JsonConvert.DeserializeObject<VerificationRequest>(message.Body.ToString());
            if (verificationRequest != null && !string.IsNullOrEmpty(verificationRequest.Email))
                return verificationRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR :GenerateVerificationCode.UnpackVerificationRequest() :: {ex.Message}");

        }
        return null!;

    }
    public string GenerateCode()
    {
        try
        {
            var rnd = new Random();
            var code = rnd.Next(100000, 999999);
            return code.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR :GenerateVerificationCode.GenerateCode() :: {ex.Message}");

        }
        return null!;
    }
    public async Task<bool> SaveVerificationRequest(VerificationRequest verificationRequest, string code)
    {
        try
        {
            using var context = _serviceProvider.GetRequiredService<DataContext>();
            var existingRequest = await context.VerificationRequests.FirstOrDefaultAsync(x => x.Email == verificationRequest.Email);
            if (existingRequest != null)
            {
                existingRequest.Code = code;
                existingRequest.ExpiryDate = DateTime.Now.AddMinutes(5);
                context.Entry(existingRequest).State = EntityState.Modified;
            }
            else
            {
                context.VerificationRequests.Add(new Data.Entities.VerificationRequestEntity() { Email = verificationRequest.Email, Code = code });

            }
            await context.SaveChangesAsync();
            return true;

        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR :GenerateVerificationCode.SaveVerificationRequest() :: {ex.Message}");

        }
        return false;

    }
}
