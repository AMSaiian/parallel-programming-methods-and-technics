using System.IO.Pipes;

namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.IpcDemo;

internal static class NamedPipeIpc
{
    internal const string PipeName = "ipc_cs_java";

    internal static async Task<long> RunAsync(long number, bool verbose)
    {
        await using var server = new NamedPipeServerStream(
            PipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        if (verbose)
        {
            Console.WriteLine($"    [NamedPipe] listening on \\\\.\\pipe\\{PipeName}, waiting for Java...");
        }

        await server.WaitForConnectionAsync();

        var sendBuf = BitConverter.GetBytes(number);
        await server.WriteAsync(sendBuf);

        var recvBuf = new byte[8];
        var read = 0;
        while (read < 8)
        {
            read += await server.ReadAsync(recvBuf.AsMemory(read, 8 - read));
        }

        return BitConverter.ToInt64(recvBuf);
    }
}
