namespace Neon.Core.Services.Encryption;

public interface IEncryptionService
{
    (string, string) Encrypt(string? value);
    string Decrypt(string? value, string? iv);
}