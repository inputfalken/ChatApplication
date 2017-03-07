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

        public static async Task StartAsync(string address, int port) {
            var listener = new TcpListener(IPAddress.Parse(address), port);
            listener.Start();
            while (true) HandleClient(await listener.AcceptTcpClientAsync());
        }

        private static async Task<string> RegisterUserAsync(TcpClient client) {
            var maybe = await TryRegisterUserAsync(client);
            await SendMessageAsync(Create(Action.Status, maybe.HasValue), client.GetStream());
            return maybe.HasValue ? maybe.Value : await RegisterUserAsync(client);
        }

        private static async Task HandleClient(TcpClient client) {
            ClientConnects?.Invoke("Client connected");
            var clientStream = client.GetStream();
            await SendMessageAsync(Create(Action.Message, "Welcome please enter your name"), clientStream);
            var userName = await RegisterUserAsync(client);
            await SendMessageAsync(Create(Action.SendMembers, UserNameToClient.Keys.ToArray()), clientStream);
            await MessageOtherClientsAsync(Create(Action.MemberJoin, userName), clientStream);
            await ChatSessionAsync(clientStream);
            await DisconnectClientAsync(userName);
        }

        private static async Task MessageOtherClientsAsync(Message message, Stream clientStream) {
            var clientsMessaged = UserNameToClient
                .Select(pair => pair.Value.GetStream())
                .Where(stream => !stream.Equals(clientStream))
                .Select(stream => SendMessageAsync(message, stream));
            await Task.WhenAll(clientsMessaged);
            ClientMessage?.Invoke(message.ToString());
        }

        private static async Task ChatSessionAsync(Stream stream) {
            var connected = true;
            using (var reader = new StreamReader(stream)) {
                while (connected) {
                    connected = (await ReadMessageAsync(reader))
                        .ToMaybe()
                        .Do(async msg => await HandleMessage(msg, stream))
                        .HasValue;
                }
            }
        }

        private static async Task HandleMessage(Message message, Stream stream) {
            // This is where the client can create requests about specifik data.
            if (message.Action == Action.MemberMessage) {
                var memberMessage = message.Parse<MemberMessage>();
                await MessageOtherClientsAsync(Create(Action.MemberMessage, memberMessage), stream);
            }
        }

        private static async Task DisconnectClientAsync(string userName) {
            var message = Create(Action.MemberDisconnect, userName);
            UserNameToClient.Remove(userName);
            await AnnounceAsync(message);
            ClientDisconects?.Invoke(message.ToString());
        }

        private static async Task AnnounceAsync(Message message) =>
            await Task.WhenAll(UserNameToClient.Select(pair => SendMessageAsync(message, pair.Value.GetStream())));


        public static event Action<string> ClientMessage;
        public static event Action<string> ClientConnects;
        public static event Action<string> ClientDisconects;

        private static async Task<Maybe<string>> TryRegisterUserAsync(TcpClient client) {
            var streamReader = new StreamReader(client.GetStream());
            return ParseMessage(await streamReader.ReadLineAsync())
                .ToMaybe()
                .Where(message => message.Action == Action.MemberJoin) // Check that the client sends the right action.
                .Select(message => Parse<string>(message.JsonObject))
                .Where(userName => !UserNameToClient.ContainsKey(userName)) // Check that the username is not taken.
                .Where(userName => userName != Server) // Check that username is not the the reserved name Server
                .Do(userName => UserNameToClient.Add(userName, client)); // Add the user to the register
        }
    }
}