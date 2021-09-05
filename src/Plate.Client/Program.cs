using Grpc.Net.Client;
using Plate.Protos;
using Grpc.Core;

var httpClient = new HttpClient(new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions { HttpClient = httpClient });

var client =  new Template.TemplateClient(channel);

await client.SyncAsync(new SyncRequest());
