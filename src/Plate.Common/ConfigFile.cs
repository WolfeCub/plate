namespace Plate.Common;

public class ConfigFile
{
    public string InputDirectory { get; set; }
    public string OutputDirectory { get; set; }

    public VaultConfig? Vault { get; set; }
}

public class VaultConfig
{
    public string Url { get; set; }
    public string Token { get; set; }
    public string MountPath { get; set; }
    public string? SecretPath { get; set; }
    public bool TlsSkipVerify { get; set; }
}
