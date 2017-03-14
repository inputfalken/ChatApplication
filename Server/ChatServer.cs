using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Functional.Maybe;
using Protocol;
using ReactiveUI;
using static Protocol.Message;
using Action = Protocol.Action;

namespace Server {
    public static class ChatServer {
        private const string Server = "Server";
        private static readonly IDictionary<string, TcpClient> Members = new Dictionary<string, TcpClient>();

        public static ISubject<string> ClientMessage { get; }
        public static ISubject<string> ClientConnects { get; }
        public static ISubject<string> ClientDisconects { get; }
        public static ISubject<string> ClientRegistered { get; }

        static ChatServer() {
            ClientMessage = new Subject<string>();
            ClientConnects = new Subject<string>();
            ClientDisconects = new Subject<string>();
            ClientRegistered = new Subject<string>();
        }


        public static async Task StartAsync(string address, int port) {
            var listener = new TcpListener(IPAddress.Parse(address), port);
            listener.Start();
            var subject = new Subject<TcpClient>();

            subject
                .Subscribe(client => ClientConnects.OnNext($"Client: {client.Client.RemoteEndPoint} connected"));

            subject
                .SelectMany(RegisterUserAsync, (client, name) => new User(name, client))
                .Subscribe(HandleRegisteredUser);

            while (true) {
                subject.OnNext(await listener.AcceptTcpClientAsync());
            }
        }

        private static async Task<string> RegisterUserAsync(TcpClient client) {
            var maybe = await TryRegisterUserAsync(client);
            await SendMessageAsync(Create(Action.Status, maybe.HasValue), client.GetStream());
            return maybe.HasValue ? maybe.Value : await RegisterUserAsync(client);
        }

        private static async void HandleRegisteredUser(User user) {
            ClientRegistered.OnNext($"{user.Client.Client.RemoteEndPoint} Sucessfully registered with {user.Name}");
            var clientStream = user.Client.GetStream();
            await SendMessageAsync(Create(Action.SendMembers, Members.Keys.ToArray()), clientStream);
            await MessageOtherClientsAsync(Create(Action.MemberJoin, user.Name), clientStream);
            await ChatSessionAsync(clientStream);
            await DisconnectClientAsync(user.Name);
        }

        private static async Task MessageOtherClientsAsync(Message message, Stream clientStream) {
            var clientsMessaged = Members
                .Select(pair => pair.Value.GetStream())
                .Where(stream => !stream.Equals(clientStream))
                .Select(stream => SendMessageAsync(message, stream));
            await Task.WhenAll(clientsMessaged);
            ClientMessage.OnNext(message.ToString());
        }

        private static async Task ChatSessionAsync(Stream stream) {
            var connected = true;
            using (var reader = new StreamReader(stream)) {
                while (connected)
                    connected = (await ReadMessageAsync(reader))
                        .ToMaybe()
                        .Do(async msg => await HandleMessage(msg, stream))
                        .HasValue;
            }
        }

        private static async Task HandleMessage(Message message, Stream stream) {
            // This is where the client can create requests about specifik data.
            if (message.Action == Action.ChatMessage) {
                var memberMessage = message.Parse<ChatMessage>();
                await MessageOtherClientsAsync(Create(Action.ChatMessage, memberMessage), stream);
            }
        }

        private static async Task DisconnectClientAsync(string userName) {
            var message = Create(Action.MemberDisconnect, userName);
            Members.Remove(userName);
            await AnnounceAsync(message);
            ClientDisconects.OnNext(message.ToString());
        }

        private static async Task AnnounceAsync(Message message) =>
            await Task.WhenAll(Members.Select(pair => SendMessageAsync(message, pair.Value.GetStream())));


        private static async Task<Maybe<string>> TryRegisterUserAsync(TcpClient client) {
            var streamReader = new StreamReader(client.GetStream());
            return ParseMessage(await streamReader.ReadLineAsync())
                .ToMaybe()
                .Where(message => message.Action == Action.MemberJoin) // Check that the client sends the right action.
                .Select(message => message.Parse<string>())
                .Where(userName => !Members.ContainsKey(userName)) // Check that the username is not taken.
                .Where(userName => userName != Server) // Check that username is not the the reserved name Server
                .Do(userName => Members.Add(userName, client)); // Add the user to the register
        }
    }

    public class User {
        public string Name { get; }
        public TcpClient Client { get; }

        public User(string name, TcpClient client) {
            Name = name;
            Client = client;
        }
    }
}