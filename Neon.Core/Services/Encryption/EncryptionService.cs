using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Models;

namespace Neon.Core.Services.Encryption;

public class EncryptionService(ILogger<EncryptionService> logger, IOptions<NeonSettings> neonSettings) : IEncryptionService
{
    private readonly NeonSettings _neonSettings = neonSettings.Value;
    
    public (string, string) Encrypt(string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        
        var key = _neonSettings.EncryptionKey;
        
        if (string.IsNullOrEmpty(key))
        {
            logger.LogError("Encryption key is not set in the configuration.");
            ArgumentException.ThrowIfNullOrEmpty(key);
        }
        
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(value);
        var encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        return (Convert.ToBase64String(encryptedBytes), Convert.ToBase64String(aes.IV));
    }

    public string Decrypt(string? value, string? iv)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        ArgumentException.ThrowIfNullOrEmpty(iv);
        
        var key = _neonSettings.EncryptionKey;
        
        if (string.IsNullOrEmpty(key))
        {
            logger.LogError("Encryption key is not set in the configuration.");
            ArgumentException.ThrowIfNullOrEmpty(key);
        }
        
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = Convert.FromBase64String(iv);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var ciphertextBytes = Convert.FromBase64String(value);
        var decryptedBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}