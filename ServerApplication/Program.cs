using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Server;

namespace ServerApplication {
    internal static class Program {
        private static void Main() {
            var address = Dns.GetHostAddresses(Dns.GetHostName())
                .Where(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork)
                .Select(ipAddress => new IPEndPoint(ipAddress, 9000))
                .First();
            Task.Run(() => ChatServer.StartAsync(address));
            ChatServer.ClientConnects.Subscribe(Console.WriteLine);
            ChatServer.ClientDisconects.Subscribe(Console.WriteLine);
            ChatServer.ClientMessage.Subscribe(Console.WriteLine);
            ChatServer.ClientRegistered.Subscribe(Console.WriteLine);
            Console.WriteLine($"Listening on {address}");
            Console.WriteLine("Press enter to kill the server");
            Console.ReadLine();
        }
    }
}