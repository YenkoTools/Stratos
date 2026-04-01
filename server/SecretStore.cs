using System.Text.Json;

namespace Server;

public class SecretStore
{
    private readonly string _secretsPath;
    private Dictionary<string, string> _keys = new();

    public SecretStore()
    {
        _secretsPath = Path.Combine(AppContext.BaseDirectory, "secrets.dat");
    }

    public bool IsInitialised => File.Exists(_secretsPath);

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void ProtectAndSave(Dictionary<string, string> keys)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(keys);
        var encrypted = System.Security.Cryptography.ProtectedData.Protect(
            json,
            optionalEntropy: null,
            scope: System.Security.Cryptography.DataProtectionScope.CurrentUser);

        File.WriteAllBytes(_secretsPath, encrypted);
        _keys = keys;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void LoadAndUnprotect()
    {
        var encrypted = File.ReadAllBytes(_secretsPath);
        var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
            encrypted,
            optionalEntropy: null,
            scope: System.Security.Cryptography.DataProtectionScope.CurrentUser);

        _keys = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted)
            ?? throw new InvalidOperationException("Failed to deserialise secrets.");
    }

    public string GetKey(string environment)
    {
        if (_keys.TryGetValue(environment.ToLowerInvariant(), out var key))
            return key;

        throw new KeyNotFoundException($"No subscription key configured for environment: {environment}");
    }
}
