using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Functional.Maybe;
using Protocol;
using static Protocol.JAction;

namespace Server {
    public static class ChatServer {
        private static readonly Dictionary<string, TcpClient> UserNameToClient = new Dictionary<string, TcpClient>();
        private const string Server = "Server";

        public static Task StartAsync(string address, int port) {
            var listener = new TcpListener(IPAddress.Parse(address), port);
            listener.Start();
            while (true) HandleClient(listener.AcceptTcpClient());
        }

        private static async Task HandleClient(TcpClient client) {
            ClientConnects?.Invoke("Client connected");
            var clientStream = client.GetStream();
            var userRegistered = await await MessageClientAsync(
                Message("Welcome please enter your name", Server),
                clientStream
            ).ContinueWith(t => RegisterUserAsync(client));
            while (!userRegistered.HasValue)
                userRegistered = await await
                    MessageClientAsync(Message(StatusFail(), Server), clientStream)
                        .ContinueWith(t => RegisterUserAsync(client));
            var userName = userRegistered.Value;

            //Send back message to approve registration
            await await MessageClientAsync(Message(StatusSucess(), Server), userName)
                .ContinueWith(successRegister => MessageClientAsync(
                        Message($"You have been sucessfully registered with the name: {userName}", Server),
                        userName
                    )
                )
                .ContinueWith(msgClient => MessageOtherClientsAsync(MemberJoins(userName), userName))
                .ContinueWith(msgOtherClient => ChatSessionAsync(userName)
                    .ContinueWith(chastSession => DisconnectClientAsync(userName)));
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
                        .Select(ParseJAction)
                        .Do(async action => await HandleAction(action, userName))
                        .HasValue;
            }
        }

        private static async Task HandleAction(JAction jAction, string userName) {
            // This is where the client can create requests about specifik data.
            if (jAction.Action == MessageAction)
                await MessageOtherClientsAsync($"{Message(jAction.Result, userName)}", userName);
        }

        private static async Task DisconnectClientAsync(string userName) {
            var message = MemberDisconnects(userName);
            UserNameToClient.Remove(userName);
            await AnnounceAsync(message);
            ClientDisconects?.Invoke(message);
        }

        private static async Task AnnounceAsync(string message) =>
            await Task.WhenAll(UserNameToClient.Select(pair => MessageClientAsync(message, pair.Key)));


        public static event Action<string> ClientMessage;
        public static event Action<string> ClientConnects;
        public static event Action<string> ClientDisconects;

        // TODO Return maybe string which HandleClient maybe handle if the client succeded
        // Could use a while loop  instead and force the user to stay
        private static async Task<Maybe<string>> RegisterUserAsync(TcpClient client) {
            var streamReader = new StreamReader(client.GetStream());
            return ParseJAction(await streamReader.ReadLineAsync())
                .ToMaybe()
                .Where(action => action.Action == NewMemberAction) // Check that the client sends the right action.
                .Where(action => !UserNameToClient.ContainsKey(action.Result)) // Check that the username is not taken.
                .Where(action => action.Result != Server) // Check that username is not the the reserved name Server
                .Do(action => UserNameToClient.Add(action.Result, client)) // Add the user to the register
                .Select(action => action.Result); // Select the successfull username
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