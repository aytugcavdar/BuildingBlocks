using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.Email;


public class EmailMessage
{
    /// <summary>
    /// Alıcı email adresi
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Alıcı adı (opsiyonel)
    /// </summary>
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? PlainTextBody { get; set; }
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
    public string? TemplateName { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
}
