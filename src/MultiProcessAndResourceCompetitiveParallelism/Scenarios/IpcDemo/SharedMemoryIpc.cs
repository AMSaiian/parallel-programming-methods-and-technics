using System.IO.MemoryMappedFiles;

namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.IpcDemo;

internal static class SharedMemoryIpc
{
    internal const string FileName = "ipc_shm.bin";

    internal static Task<long> RunAsync(long number, string javaDir, bool verbose)
    {
        var filePath = Path.Combine(javaDir, FileName);

        var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var mmf = MemoryMappedFile.CreateFromFile(fs, null, 16, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: false);
        using var accessor = mmf.CreateViewAccessor(0, 16);

        accessor.Write(0, number);
        Thread.MemoryBarrier();
        accessor.Write(8, (byte)1);

        if (verbose)
        {
            Console.WriteLine($"    [SharedMemory] wrote number={number}, waiting for Java response...");
        }

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (accessor.ReadByte(9) == 0)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Java process did not set the response flag within 30 s.");
            }

            Thread.Sleep(10);
        }

        Thread.MemoryBarrier();
        accessor.Read(0, out long received);
        return Task.FromResult(received);
    }
}
