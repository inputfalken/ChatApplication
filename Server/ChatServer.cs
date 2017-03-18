﻿using System;
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
using static Protocol.Message;
using Action = Protocol.Action;

namespace Server {
    public static class ChatServer {
        private const string Server = "Server";
        private static readonly ISet<User> Users = new HashSet<User>(User.Comparer);
        private static readonly ISubject<string> SubjectClientRegistered;
        private static readonly ISubject<string> SubjectClientDisconects;
        private static readonly ISubject<string> SubjectClientConnects;
        private static readonly ISubject<string> SubjectClientMessage;

        static ChatServer() {
            SubjectClientMessage = new Subject<string>();
            SubjectClientConnects = new Subject<string>();
            SubjectClientDisconects = new Subject<string>();
            SubjectClientRegistered = new Subject<string>();
        }

        public static IObservable<string> ClientMessage => SubjectClientMessage;

        public static IObservable<string> ClientConnects => SubjectClientConnects;

        public static IObservable<string> ClientDisconects => SubjectClientDisconects;

        public static IObservable<string> ClientRegistered => SubjectClientRegistered;


        public static async Task StartAsync(IPEndPoint ipEndPoint) {
            var listener = new TcpListener(ipEndPoint);
            listener.Start();
            var clientJoins = new Subject<TcpClient>();

            clientJoins
                .Subscribe(client => SubjectClientConnects.OnNext($"Client: {client.Client.RemoteEndPoint} connected"));

            clientJoins
                .SelectMany(RegisterUserAsync)
                .Subscribe(HandleRegisteredUser);

            while (true) clientJoins.OnNext(await listener.AcceptTcpClientAsync());
        }

        private static async Task<User> RegisterUserAsync(TcpClient client) {
            var maybe = await TryRegisterUserAsync(client);
            await SendMessageAsync(Create(Action.Status, maybe.HasValue), client.GetStream());
            return maybe.HasValue ? maybe.Value : await RegisterUserAsync(client);
        }

        private static async void HandleRegisteredUser(User user) {
            SubjectClientRegistered.OnNext(
                $"{user.Client.Client.RemoteEndPoint} Sucessfully registered with {user.Name}");
            var clientStream = user.Client.GetStream();
            var message = ParseMessage(await new StreamReader(clientStream).ReadLineAsync()); // Temp solution
            if (message.Action == Action.Chat) {
                await SendMessageAsync(Create(Action.SendMembers, Users.Select(u => u.Name).ToArray()), clientStream);
                await MessageOtherClientsAsync(Create(Action.MemberJoin, user.Name), clientStream);
                await ChatSessionAsync(clientStream);
            }
            else {
                await SendMessageAsync(
                    Create(Action.ChatMessage,
                        new ChatMessage(Server, "You can only request a chat session at this time")), clientStream);
            }
            await DisconnectClientAsync(user);
        }

        private static async Task MessageOtherClientsAsync(Message message, Stream clientStream) {
            var clientsMessaged = Users
                .Select(user => user.Client.GetStream())
                .Where(stream => !stream.Equals(clientStream))
                .Select(stream => SendMessageAsync(message, stream));
            await Task.WhenAll(clientsMessaged);
            SubjectClientMessage.OnNext(message.ToString());
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
            switch (message.Action) {
                case Action.ChatMessage:
                    var memberMessage = message.Parse<ChatMessage>();
                    await MessageOtherClientsAsync(Create(Action.ChatMessage, memberMessage), stream);
                    break;
                case Action.PrivateChatMessage:
                    var pm = message.Parse<PrivateMessage>();

                    var maybeUser = Users //Maybe a user
                        .SingleOrDefault(user => user.Name.Equals(pm.Recipent)) // Could be null
                        .ToMaybe() // Do the following functions as long as user is not null
                        .Select(user => user.Client.GetStream());

                    await maybeUser //TODO Replace personal ChatMessage with a PrivateMessage.
                        .Where(rStream => rStream.Equals(stream))
                        .SelectOrElse(
                            rStream => SendPm(rStream, "You just whispered yourself", Server),
                            //Client whispered himself
                            () => maybeUser.SelectOrElse(
                                rStream => SendPm(rStream, pm.Message, pm.Recipent),
                                // Client whispered a different client
                                () => SendPm(stream, "PM: There's no user with that name", Server)
                                // Client whispered a none existing client.
                            )
                        );

                    break;
            }
        }


        private static Task SendPm(Stream recipentStream, string message, string recipent)
            => SendMessageAsync(Create(Action.ChatMessage, new ChatMessage(recipent, message)), recipentStream);

        private static async Task DisconnectClientAsync(User user) {
            var message = Create(Action.MemberDisconnect, user.Name);
            lock (Users) {
                Users.Remove(user);
            }
            await AnnounceAsync(message);
            SubjectClientDisconects.OnNext(message.ToString());
        }

        private static async Task AnnounceAsync(Message message) =>
            await Task.WhenAll(Users.Select(pair => SendMessageAsync(message, pair.Client.GetStream())));


        private static async Task<Maybe<User>> TryRegisterUserAsync(TcpClient client) {
            var streamReader = new StreamReader(client.GetStream());
            return ParseMessage(await streamReader.ReadLineAsync())
                .ToMaybe()
                .Where(message => message.Action == Action.MemberJoin) // Check that the client sends the right action.
                .Select(message => message.Parse<string>())
                .Where(userName => userName != Server) // Check that username is not the the reserved name Server
                .Select(userName => new User(userName, client))
                .Where(user => {
                    lock (Users) {
                        return Users.Add(user);
                    }
                });
        }

        public class User {
            public User(string name, TcpClient client) {
                Name = name;
                Client = client;
            }

            public string Name { get; }
            public TcpClient Client { get; }
            public static UserComparer Comparer { get; } = new UserComparer();

            public class UserComparer : IEqualityComparer<User> {
                public bool Equals(User x, User y) => x.Name == y.Name;

                public int GetHashCode(User obj) => obj.Name.GetHashCode();
            }
        }
    }
}