using System.Net;
using System.Net.Sockets;

namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.IpcDemo;

internal static class TcpSocketIpc
{
    internal const int Port = 12500;

    internal static async Task<long> RunAsync(long number, bool verbose)
    {
        var listener = new TcpListener(IPAddress.Loopback, Port);
        listener.Start();

        if (verbose)
        {
            Console.WriteLine($"    [TCP] listening on port {Port}, waiting for Java...");
        }

        using var client = await listener.AcceptTcpClientAsync();
        listener.Stop();

        await using var stream = client.GetStream();

        await stream.WriteAsync(BitConverter.GetBytes(number));

        var recvBuf = new byte[8];
        var read = 0;
        while (read < 8)
        {
            read += await stream.ReadAsync(recvBuf.AsMemory(read, 8 - read));
        }

        return BitConverter.ToInt64(recvBuf);
    }
}
