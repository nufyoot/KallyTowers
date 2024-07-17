using System.Net;
using System.Net.Sockets;

var tcpListener = new TcpListener(IPAddress.Any, 9090);
tcpListener.Start();

while (true)
{
    var client = tcpListener.AcceptTcpClient();
    var stream = client.GetStream();

    ReadOnlySpan<byte> data;

    data = "This is a test"u8;
    stream.Write(data);
    Console.WriteLine("Connected and sent init data");
}