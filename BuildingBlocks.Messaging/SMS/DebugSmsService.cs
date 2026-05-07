using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.SMS;

public class DebugSmsService : ISmsService
{
    private readonly ILogger<DebugSmsService> _logger;

    public DebugSmsService(ILogger<DebugSmsService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        // Gerçek SMS atmıyoruz, log dosyasına veya konsola yazıyoruz (Simülasyon)
        _logger.LogInformation("\n************** SMS SİMÜLASYONU **************");
        _logger.LogInformation("📱 Kime: {PhoneNumber}", phoneNumber);
        _logger.LogInformation("💬 Mesaj: {Message}", message);
        _logger.LogInformation("*********************************************\n");

        return Task.FromResult(true);
    }
}