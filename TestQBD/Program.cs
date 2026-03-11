using QBlockData.Sockets.TLVSockets;
using System.Net;

TLVSocketServer server = new(System.Net.Sockets.ProtocolType.Tcp);
server.IsCheckedOnlineTime = true;
server.StartListen(10086);

TLVSocketClient client = new(System.Net.Sockets.ProtocolType.Tcp);

client.StartConnect(IPEndPoint.Parse("192.168.3.117:10086"));
client.ServerPingPacketReceived += Client_ServerPingPacketReceived;

void Client_ServerPingPacketReceived(TLVSocketClient obj)
{
    Console.WriteLine("A");
}

while (true)
{

}