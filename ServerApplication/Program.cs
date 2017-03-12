using System;
using System.Threading.Tasks;
using Server;

namespace ServerApplication {
    internal static class Program {
        private static void Main() {
            Task.Run(() => ChatServer.StartAsync("10.0.2.15", 23000));
            ChatServer.ClientConnects.Subscribe(Console.WriteLine);
            ChatServer.ClientDisconects.Subscribe(Console.WriteLine);
            ChatServer.ClientMessage.Subscribe(Console.WriteLine);
            Console.WriteLine("Press enter to kill the server");
            Console.ReadLine();
        }
    }
}