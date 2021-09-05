using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Plate.Common;

public class ConfigFileFactory
{
    private readonly IDeserializer _deserializer;

    public ConfigFileFactory()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

    }

    public async Task<ConfigFile> ReadConfigFile()
    {
        var path = Environment.GetEnvironmentVariable("PLATE_CONFIG_PATH") ?? "/etc/plate.yml";
        var content = await File.ReadAllTextAsync(path);
        return _deserializer.Deserialize<ConfigFile>(content);
    }
}
