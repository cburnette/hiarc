using System;
using System.Threading.Tasks;
using HiarcGRPC;
using Grpc.Net.Client;
using System.Runtime.InteropServices;

namespace HiarcGRPCIntegrationTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serverAddress = "https://localhost:5001";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                // The following statement allows you to call insecure services. To be used only in development environments.
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                serverAddress = "http://localhost:5000";
            }

            var channel = GrpcChannel.ForAddress(serverAddress);
            var client = new HiarcService.HiarcServiceClient(channel);

            var initDBRequest = new InitDatabaseRequest();
            var reply = await client.InitDatabaseAsync(initDBRequest);
            Console.WriteLine(reply.Result.Message);
        }
    }
}
