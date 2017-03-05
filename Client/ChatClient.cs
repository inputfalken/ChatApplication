using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Protocol;
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
            var message = Message.ParseJAction(await new StreamReader(_client.GetStream()).ReadLineAsync());
            return Message.Parse<string>(message.JsonObject);
        }

        public async Task SendMessage(string message, string userName) {
            await MessageAsync(Message.Create(Action.MemberMessage, new MemberMessage(userName, message)));
        }

        public event Action<string> MessageRecieved;
        public event Action<string> NewMember;
        public event Action<string> MemberDisconnect;
        public event Action<IReadOnlyList<string>> FetchMembers;

        public async Task Listen() {
            var data = await new StreamReader(_client.GetStream()).ReadLineAsync();
            var message = Message.ParseJAction(data);
            switch (message.Action) {
                case Action.MemberJoin:
                    NewMember?.Invoke(Message.Parse<string>(message.JsonObject));
                    break;
                case Action.SendMembers:
                    FetchMembers?.Invoke(Message
                        .Parse<IReadOnlyList<string>>(message.JsonObject)
                    );
                    break;
                case Action.MemberMessage:
                    var memberMessage = Message.Parse<MemberMessage>(message.JsonObject);
                    MessageRecieved?.Invoke($"{memberMessage.UserName}: {memberMessage.Message}");
                    break;
                case Action.Message:
                    break;
                case Action.Status:
                    break;
                case Action.MemberDisconnect:
                    MemberDisconnect?.Invoke(Message.Parse<string>(message.JsonObject));
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
            await MessageAsync(Message.Create(Action.MemberJoin, userName));
            var data = await new StreamReader(_client.GetStream()).ReadLineAsync();
            var action = Message.ParseJAction(data);
            if (action.Action == Action.Status) return Message.Parse<bool>(action.JsonObject);
            throw new IOException($"Expected: {Action.Status}, Was:{action.Action}");
        }

        public void CloseConnection() {
            _client.GetStream().Close();
            _client.Close();
        }
    }
}