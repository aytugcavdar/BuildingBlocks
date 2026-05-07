using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.SMS;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
