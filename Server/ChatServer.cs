using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Functional.Maybe;
using Protocol;
using static Protocol.Message;
using Action = Protocol.Action;

namespace Server {
    public static class ChatServer {
        private const string Server = "Server";
        private static readonly Dictionary<string, TcpClient> UserNameToClient = new Dictionary<string, TcpClient>();

        public static Task StartAsync(string address, int port) {
            var listener = new TcpListener(IPAddress.Parse(address), port);
            listener.Start();
            while (true) HandleClient(listener.AcceptTcpClient());
        }

        private static async Task<string> RegisterUserNameAsync(TcpClient client) {
            var maybe = await RegisterUserAsync(client);
            await MessageClientAsync(Create(Action.Status, maybe.HasValue), client.GetStream());
            return maybe.HasValue ? maybe.Value : await RegisterUserNameAsync(client);
        }

        private static async Task HandleClient(TcpClient client) {
            ClientConnects?.Invoke("Client connected");
            var clientStream = client.GetStream();
            await MessageClientAsync(Create(Action.Message, "Welcome please enter your name"), clientStream);
            var userName = await RegisterUserNameAsync(client);
            await MessageClientAsync(Create(Action.SendMembers, UserNameToClient.Keys.ToArray()), userName);
            await MessageOtherClientsAsync(Create(Action.MemberJoin, userName), userName);
            await ChatSessionAsync(userName);
            await DisconnectClientAsync(userName);
        }

        private static async Task MessageOtherClientsAsync(string message, string userName) {
            var clientsMessaged = UserNameToClient
                .Where(pair => !pair.Key.Equals(userName))
                .Select(pair => pair.Value.GetStream())
                .Select(stream => MessageClientAsync(message, stream));
            await Task.WhenAll(clientsMessaged);
            ClientMessage?.Invoke(message);
        }

        private static async Task ChatSessionAsync(string userName) {
            using (var stream = UserNameToClient[userName].GetStream()) {
                var streamReader = new StreamReader(stream);
                var connected = true;
                while (connected)
                    connected = (await streamReader.ReadLineAsync())
                        .ToMaybe()
                        .Select(ParseMessage)
                        .Do(async action => await HandleAction(action, userName))
                        .HasValue;
            }
        }

        private static async Task HandleAction(Message message, string userName) {
            // This is where the client can create requests about specifik data.
            if (message.Action == Action.MemberMessage) {
                var memberMessage = message.Parse<MemberMessage>();
                await MessageOtherClientsAsync(Create(Action.MemberMessage, memberMessage), userName);
            }
        }

        private static async Task DisconnectClientAsync(string userName) {
            var message = Create(Action.MemberDisconnect, userName);
            UserNameToClient.Remove(userName);
            await AnnounceAsync(message);
            ClientDisconects?.Invoke(message);
        }

        private static async Task AnnounceAsync(string message) =>
            await Task.WhenAll(UserNameToClient.Select(pair => MessageClientAsync(message, pair.Key)));


        public static event Action<string> ClientMessage;
        public static event Action<string> ClientConnects;
        public static event Action<string> ClientDisconects;

        private static async Task<Maybe<string>> RegisterUserAsync(TcpClient client) {
            var streamReader = new StreamReader(client.GetStream());
            return ParseMessage(await streamReader.ReadLineAsync())
                .ToMaybe()
                .Where(message => message.Action == Action.MemberJoin) // Check that the client sends the right action.
                .Select(message => Parse<string>(message.JsonObject))
                .Where(userName => !UserNameToClient.ContainsKey(userName)) // Check that the username is not taken.
                .Where(userName => userName != Server) // Check that username is not the the reserved name Server
                .Do(userName => UserNameToClient.Add(userName, client)); // Add the user to the register
        }


        private static async Task MessageClientAsync(string message, Stream stream) {
            var buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task MessageClientAsync(string message, string sender) {
            var stream = UserNameToClient[sender].GetStream();
            var buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}