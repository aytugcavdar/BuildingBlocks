using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
namespace BuildingBlocks.Security.Encryption;

public static class HashingHelper
{

    public static void CreatePasswordHash(
        string password,
        out byte[] passwordHash,
        out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }


    public static bool VerifyPasswordHash(
        string password,
        byte[] passwordHash,
        byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512(passwordSalt);
        byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(passwordHash);
    }
}
