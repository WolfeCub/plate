using Grpc.Net.Client;
using Plate.Protos;

using var channel = GrpcChannel.ForAddress("http://localhost:5000");

var client =  new Template.TemplateClient(channel);

await client.SyncAsync(new SyncRequest());
