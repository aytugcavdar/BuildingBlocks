using BuildingBlocks.Messaging.Email;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging.Templates;

/// <summary>
/// HTML email template servisi.
/// Template dosyalarında {{Key}} placeholder sözdizimi desteklenir.
/// Template dosyaları EmailOptions.TemplateFolder'dan okunur.
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly string _templateBasePath;

    public TemplateService(IOptions<EmailOptions> options)
    {
        _templateBasePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            options.Value.TemplateFolder);
    }

    public async Task<string> RenderTemplateAsync(
        string templateName,
        Dictionary<string, object> data)
    {
        var templatePath = Path.Combine(_templateBasePath, $"{templateName}.html");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException(
                $"Email template '{templateName}.html' not found. " +
                $"Check that it exists in: {_templateBasePath}");

        var template = await File.ReadAllTextAsync(templatePath);

        foreach (var kvp in data)
        {
            // Desteklenen placeholder formatları: {{Key}} ve {Key}
            var doubleBrace = $"{{{{{kvp.Key}}}}}";
            var singleBrace = $"{{{kvp.Key}}}";
            var value = kvp.Value?.ToString() ?? string.Empty;

            template = template.Replace(doubleBrace, value);
            template = template.Replace(singleBrace, value);
        }

        // Kalan işlenmemiş placeholder'ları boş bırak
        return template;
    }
}
