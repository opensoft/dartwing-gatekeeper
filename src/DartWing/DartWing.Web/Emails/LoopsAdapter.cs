using System.Text.Json;

namespace DartWing.Web.Emails;

public class LoopsAdapter(HttpClient httpClient, ILogger<LoopsAdapter> logger)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<bool> SendTransactionalEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        
        logger.LogDebug(LogMessages.SendingEmail, message.TransactionalId, message.Email);

        try
        {
            using var response = await httpClient.PostAsJsonAsync("transactional", message, JsonSerializerOptions, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<LoopsResponse>(cancellationToken);
            if (result is {Success: true})
            {
                logger.LogDebug(LogMessages.EmailSent, message.TransactionalId, message.Email);
                return true;
            }
            
            logger.LogError(LogMessages.RequestError, result?.Error.Reason);
            
            return false;
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, LogMessages.RequestError, e.Message);

            return false;
        }
    }

    private static class LogMessages
    {
        public const string SendingEmail = "Sending transactional email with ID {TransactionalId} to {Email}.";
        public const string EmailSent = "Transactional email with ID {TransactionalId} sent to {Email}.";
        public const string RequestError = "Request error white sending email: {Message}";
    }

    private class LoopsResponse
    {
        public bool Success { get; set; }
        public ErrorDetails Error { get; set; }
    }

    private class ErrorDetails
    {
        public string TransactionalId { get; set; }
        public string Reason { get; set; }
    }
}