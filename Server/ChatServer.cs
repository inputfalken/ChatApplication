using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Functional.Maybe;
using Protocol;

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
            var clientStream = client.GetStream();
            var welcomeMessageSent = MessageClientAsync(
                JAction.Message("Welcome please enter your name", Server).ToString(),
                clientStream
            );
            ClientConnects?.Invoke("Client connected");
            await welcomeMessageSent;
            var maybeName = await RegisterUserAsync(client);
            while (!maybeName.HasValue)
                maybeName = await await
                    MessageClientAsync(JAction.StatusFail().ToString(), clientStream)
                        .ContinueWith(t => RegisterUserAsync(client));
            var userName = maybeName.Value;

            //Send back message to approve registration
            await MessageClientAsync(JAction.StatusSucess().ToString(), userName);
            var writeMessageAsync = MessageClientAsync(
                JAction.Message($"You have been sucessfully registered with the name: {maybeName}", Server)
                    .ToString(),
                clientStream
            );
            var messageClientsExcept = MessageOtherClientsAsync(
                JAction.MemberJoins(userName).ToString(),
                userName
            );
            await writeMessageAsync;
            await messageClientsExcept;
            await Task.Run(() => ChatSessionAsync(userName));
            await DisconnectClientAsync(userName);
        }

        private static Maybe<string> Command(string command, string userName) {
            switch (command) {
                case "-members":
                    return UserNameToClient.Select(pair => pair.Key).Aggregate((s, s1) => $"{s}\n{s1}").ToMaybe();
                case "-whoami":
                    return userName.ToMaybe();
                default:
                    return Maybe<string>.Nothing;
            }
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
                        .Do(message => ParseMessage(message, userName))
                        .HasValue;
            }
        }

        private static void ParseMessage(string line, string userName) {
            Command(line, userName)
                .Match(
                    async cmdResponse => await MessageClientAsync(cmdResponse, userName),
                    async () =>
                        await MessageOtherClientsAsync($"{JAction.Message(line, userName)}", userName)
                );
        }

        private static async Task DisconnectClientAsync(string userName) {
            var message = JAction.MemberDisconnects(userName).ToString();
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
            return JAction.ParseToJAction(await streamReader.ReadLineAsync())
                .ToMaybe()
                .Where(action => action.Action == JAction.NewMemberAction)
                .Where(action => !UserNameToClient.ContainsKey(action.Result))
                .Do(action => UserNameToClient.Add(action.Result, client))
                .Select(action => action.Result);
        }


        private static async Task MessageClientAsync(string message, Stream stream) {
            var buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static async Task MessageClientAsync(string message, string userName) {
            var stream = UserNameToClient[userName].GetStream();
            var buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}