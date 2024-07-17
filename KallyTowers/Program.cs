using System.Net;
using System.Net.Sockets;

namespace KallyTowers;

public static class Program
{
    public static void Main()
    {
        var connectionToMud = new TcpClient("localhost", 9999);
        var stream = connectionToMud.GetStream();
        
    }
}