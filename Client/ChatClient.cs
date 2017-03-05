using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Protocol;
using static Protocol.Message;
using Action = Protocol.Action;

namespace Client {
    public class ChatClient {
        private readonly TcpClient _client;
        private readonly string _ip;
        private readonly int _port;


        public ChatClient(string ip, int port) {
            _ip = ip;
            _port = port;
            _client = new TcpClient();
        }

        public async Task Connect() => await _client.ConnectAsync(IPAddress.Parse(_ip), _port);

        public async Task<string> ReadMessage() {
            var message = ParseMessage(await new StreamReader(_client.GetStream()).ReadLineAsync());
            return Parse<string>(message.JsonObject);
        }

        public async Task SendMessage(string message, string userName)
            => await MessageAsync(Create(Action.MemberMessage, new MemberMessage(userName, message)));

        public event Action<string> MessageRecieved;
        public event Action<string> NewMember;
        public event Action<string> MemberDisconnect;
        public event Action<IReadOnlyList<string>> FetchMembers;

        public async Task Listen() {
            var data = await new StreamReader(_client.GetStream()).ReadLineAsync();
            var message = ParseMessage(data);
            switch (message.Action) {
                case Action.MemberJoin:
                    NewMember?.Invoke(message.Parse<string>());
                    break;
                case Action.SendMembers:
                    FetchMembers?.Invoke(message.Parse<IReadOnlyList<string>>());
                    break;
                case Action.MemberMessage:
                    var memberMessage = message.Parse<MemberMessage>();
                    MessageRecieved?.Invoke($"{memberMessage.UserName}: {memberMessage.Message}");
                    break;
                case Action.Message:
                    break;
                case Action.Status:
                    break;
                case Action.MemberDisconnect:
                    MemberDisconnect?.Invoke(message.Parse<string>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task MessageAsync(string message) {
            var buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            await _client.GetStream().WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task<bool> Register(string userName) {
            await MessageAsync(Create(Action.MemberJoin, userName));
            var data = await new StreamReader(_client.GetStream()).ReadLineAsync();
            var action = ParseMessage(data);
            if (action.Action == Action.Status) return Parse<bool>(action.JsonObject);
            throw new IOException($"Expected: {Action.Status}, Was:{action.Action}");
        }

        public void CloseConnection() {
            _client.GetStream().Close();
            _client.Close();
        }
    }
}