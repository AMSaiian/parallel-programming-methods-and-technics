import java.io.*;
import java.net.*;
import java.nio.*;
import java.nio.channels.*;
import java.nio.file.*;
import java.util.logging.*;

public class IpcServer {

    private static final Logger log = Logger.getLogger("IpcServer");

    private static final long READY_TIMEOUT_MS = 30_000;
    private static final long RETRY_INTERVAL_MS = 25;

    static {
        System.setProperty("java.util.logging.SimpleFormatter.format",
            "[%1$tT] [Java/%4$s] %5$s%n");
        Logger.getLogger("").setLevel(Level.ALL);
        for (Handler h : Logger.getLogger("").getHandlers()) {
            h.setLevel(Level.ALL);
        }
    }

    public static void main(String[] args) throws Exception {
        if (args.length < 2) {
            log.severe("Usage: IpcServer <method> <param>");
            return;
        }

        String method = args[0].toLowerCase();
        String param = args[1];

        log.info("method=" + method + " param=" + param);

        try {
            switch (method) {
                case "sharedmemory" -> sharedMemory(param);
                case "namedpipe" -> namedPipe(param);
                case "tcp" -> tcp(Integer.parseInt(param));
                default -> log.severe("Unknown method: " + method);
            }
        } catch (Exception ex) {
            log.severe("IPC error: " + ex);
        }
    }

    private static void sharedMemory(String filePath) throws Exception {
        Path path = Paths.get(filePath);
        long deadline = System.currentTimeMillis() + READY_TIMEOUT_MS;
        while (!Files.exists(path) || Files.size(path) < 16) {
            if (System.currentTimeMillis() > deadline) {
                throw new IOException("Shared-memory file " + filePath + " not ready within " + READY_TIMEOUT_MS + " ms");
            }
            Thread.sleep(RETRY_INTERVAL_MS);
        }

        try (RandomAccessFile raf = new RandomAccessFile(filePath, "rw");
             FileChannel ch = raf.getChannel()) {

            MappedByteBuffer buf = ch.map(FileChannel.MapMode.READ_WRITE, 0, 16);
            buf.order(ByteOrder.LITTLE_ENDIAN);

            while (buf.get(8) == 0) {
                Thread.sleep(5);
                buf.load();
            }

            long number = buf.getLong(0);
            log.info("[SharedMemory] received number=" + number);

            buf.put(9, (byte) 1);
            buf.force();
        }
    }

    private static void namedPipe(String pipeName) throws Exception {
        String path = "\\\\.\\pipe\\" + pipeName;
        try (RandomAccessFile pipe = openWithRetry(path)) {
            byte[] inBuf = new byte[8];
            pipe.readFully(inBuf);

            long number = ByteBuffer.wrap(inBuf).order(ByteOrder.LITTLE_ENDIAN).getLong();
            log.info("[NamedPipe] received number=" + number);

            byte[] outBuf = ByteBuffer.allocate(8)
                .order(ByteOrder.LITTLE_ENDIAN)
                .putLong(number)
                .array();
            pipe.write(outBuf);
        }
    }

    private static void tcp(int port) throws Exception {
        try (Socket socket = connectWithRetry("127.0.0.1", port);
             InputStream in = socket.getInputStream();
             OutputStream out = socket.getOutputStream()) {

            byte[] inBuf = new byte[8];
            int offset = 0;
            while (offset < 8) {
                int n = in.read(inBuf, offset, 8 - offset);
                if (n == -1) throw new EOFException("Connection closed before 8 bytes received");
                offset += n;
            }

            long number = ByteBuffer.wrap(inBuf).order(ByteOrder.LITTLE_ENDIAN).getLong();
            log.info("[TCP] received number=" + number);

            out.write(ByteBuffer.allocate(8).order(ByteOrder.LITTLE_ENDIAN).putLong(number).array());
            out.flush();
        }
    }

    private static RandomAccessFile openWithRetry(String path) throws Exception {
        long deadline = System.currentTimeMillis() + READY_TIMEOUT_MS;
        FileNotFoundException last = null;
        while (System.currentTimeMillis() <= deadline) {
            try {
                return new RandomAccessFile(path, "rw");
            } catch (FileNotFoundException ex) {
                last = ex;
                Thread.sleep(RETRY_INTERVAL_MS);
            }
        }
        throw new IOException("Named pipe " + path + " not ready within " + READY_TIMEOUT_MS + " ms", last);
    }

    private static Socket connectWithRetry(String host, int port) throws Exception {
        long deadline = System.currentTimeMillis() + READY_TIMEOUT_MS;
        IOException last = null;
        while (System.currentTimeMillis() <= deadline) {
            try {
                return new Socket(host, port);
            } catch (ConnectException ex) {
                last = ex;
                Thread.sleep(RETRY_INTERVAL_MS);
            }
        }
        throw new IOException("TCP listener " + host + ":" + port + " not ready within " + READY_TIMEOUT_MS + " ms", last);
    }
}
