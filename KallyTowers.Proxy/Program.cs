using System.Net;
using System.Net.Sockets;

namespace KallyTowers.Proxy;

public static class Program
{
    public static async Task Main()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var listeningSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        listeningSocket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 9999));
        listeningSocket.Listen();

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var incomingConnection = await listeningSocket.AcceptAsync(cancellationTokenSource.Token);
            HandleIncomingConnection(incomingConnection, cancellationTokenSource.Token);
        }
    }

    private static async void HandleIncomingConnection(Socket incomingConnection, CancellationToken cancellationToken)
    {
        var mudConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var log = new FileStream("output.log", FileMode.Create);

        try
        {
            await mudConnection.ConnectAsync("t2tmud.org", 9999, cancellationToken);
            await SendMessageAsync(incomingConnection, $"Successfully connected to {mudConnection.RemoteEndPoint}\n", cancellationToken);
        }
        catch (SocketException e)
        {
            await SendMessageAsync(incomingConnection, $"An exception was thrown while connecting to {mudConnection.RemoteEndPoint}:\n{e}\n", cancellationToken);
            return;
        }

        await Task.WhenAll(
            HandleSocketData("--CLIENT--\n",incomingConnection, mudConnection, log, cancellationToken),
            HandleSocketData("--MUD--\n",mudConnection, incomingConnection, log, cancellationToken)
        );
    }

    private static async Task SendMessageAsync(Socket socket, string message, CancellationToken cancellationToken)
    {
        await socket.SendAsync(System.Text.Encoding.UTF8.GetBytes(message), cancellationToken);
    }
    
    private static async Task SendDataAsync(Socket socket, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await socket.SendAsync(data, cancellationToken);
    }

    private static async Task LogMessageAsync(string message, Stream log, CancellationToken cancellationToken)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(message);
        await LogDataAsync(data, log, cancellationToken);
    }

    private static async Task LogDataAsync(ReadOnlyMemory<byte> data, Stream log, CancellationToken cancellationToken)
    {
        await log.WriteAsync(data, cancellationToken);
        await log.FlushAsync(cancellationToken);
    }

    private static async Task HandleSocketData(string logPrefix, Socket ourConnection, Socket theirConnection, Stream log, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var length = await ourConnection.ReceiveAsync(buffer, cancellationToken);

            if (length == 0)
            {
                theirConnection.Close();
                return;
            }
            
            await SendDataAsync(theirConnection, buffer.AsMemory()[0..length], cancellationToken);
            await LogMessageAsync(logPrefix, log, cancellationToken);
            await LogDataAsync(buffer.AsMemory()[0..length], log, cancellationToken);
        }
    }
}
