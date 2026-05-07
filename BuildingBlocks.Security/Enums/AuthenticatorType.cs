using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Security.Enums;

public enum AuthenticatorType
{
    None = 0,

    Email = 1,

    Sms = 2,

    Otp = 3
}