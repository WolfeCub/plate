using Grpc.Core;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Plate.Common;
using Plate.Protos;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using static Plate.Protos.SyncReply.Types;

namespace Plate.Server.Services;

public class TemplateService : Template.TemplateBase
{
    private readonly ILogger<TemplateService> _logger;
    private readonly ConfigFile _config;

    public TemplateService(
        ILogger<TemplateService> logger,
        ConfigFile config
    )
    {
        _logger = logger;
        _config = config;
    }

    public override async Task Sync(SyncRequest request, IServerStreamWriter<SyncReply> responseStream, ServerCallContext context)
    {
        try
        {
            await DoWork(responseStream);
        }
        catch (Exception e)
        {
            await responseStream.WriteAsync(new SyncReply { Status = EnumStatus.Failure });
            throw;
        }
    }

    private async Task DoWork(IServerStreamWriter<SyncReply> responseStream)
    {
        var matcher = new Matcher();
        matcher.AddInclude("**/*");

        var vaultClient = InitVaultClient();
        var vars = await GetVars(vaultClient);

        var dirWrapper = new DirectoryInfoWrapper(new DirectoryInfo(_config.InputDirectory));
        var renderedFiles = matcher
            .Execute(dirWrapper).Files
            .Select(f => (Path: f.Path,
                          Content: Scriban.Template.Parse(File.ReadAllText(Path.Join(_config.InputDirectory, f.Path)))
                                                    .Render(vars)))
            .ToList();

        await Task.WhenAll(renderedFiles.Select(async (f) =>
        {
            var (path, content) = f;

            var outputPath = Path.Join(_config.OutputDirectory, path);
            var parent = Directory.GetParent(outputPath);

            if (!(parent?.Exists ?? false))
                Directory.CreateDirectory(parent!.FullName);

            await File.WriteAllTextAsync(outputPath, content);

            await responseStream.WriteAsync(new SyncReply { TemplatePath = outputPath });
        }));
    }

    public VaultClient InitVaultClient()
    {
        var authMethod = new TokenAuthMethodInfo(_config.Vault.Token);
        var vaultClientSettings = new VaultClientSettings(_config.Vault.Url, authMethod)
        {
            MyHttpClientProviderFunc = (messageHandler) =>
            {
                var handler = new HttpClientHandler();

                if (_config.Vault.TlsSkipVerify)
                    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                return new HttpClient(handler);
            }
        };
        return new VaultClient(vaultClientSettings);

    }

    public async Task<object> GetVars(IVaultClient vaultClient)
    {
        var rootSecretsResponse = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(
            path: _config.Vault.SecretPath ?? "",
            mountPoint: _config.Vault.MountPath
        );
        var rootSecrets = rootSecretsResponse.Data.Keys;

        var secretResponses = await Task.WhenAll(
            rootSecrets.Select(
                async (path) => KeyValuePair.Create(
                    path,
                    await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: path, mountPoint: _config.Vault.MountPath)
            )));

        var foo = secretResponses.ToDictionary(
            k => k.Key,
            v => v.Value.Data.Data
        );

        return foo;
    }
}
