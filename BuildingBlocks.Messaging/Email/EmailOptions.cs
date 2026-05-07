namespace BuildingBlocks.Messaging.Email;

public class EmailOptions
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string TemplateFolder { get; set; } = "EmailTemplates";
    public bool SendEmailInDevelopment { get; set; } = false;

    /// <summary>
    /// Uygulamanın base URL'i.
    /// Email confirmation ve password reset linklerinde kullanılır.
    /// Örnek: "https://myapp.com" (sonda / olmadan)
    /// </summary>
    public string AppBaseUrl { get; set; } = string.Empty;
}
