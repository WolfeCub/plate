using Grpc.Core;
using Grpc.Net.Client;
using Plate.Protos;
using static Plate.Protos.SyncReply.Types;

var httpClient = new HttpClient(new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var channel = GrpcChannel.ForAddress(
    Environment.GetEnvironmentVariable("PLATE_SERVER_URL") ?? "https://localhost:5001",
    new GrpcChannelOptions { HttpClient = httpClient }
);

var client =  new Template.TemplateClient(channel);
var stream = client.Sync(new SyncRequest());

await foreach (var response in stream.ResponseStream.ReadAllAsync())
{
    if (response.Status == EnumStatus.Failure)
    {
        Console.WriteLine("Something went wrong. Check the server logs for more details", Console.Error);
        return;
    }

    Console.WriteLine($"Templating: {response.TemplatePath}");
}
