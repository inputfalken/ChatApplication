using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Stream _stream;


        public ChatClient(string ip, int port) {
            _ip = ip;
            _port = port;
            _client = new TcpClient();
        }

        public async Task Connect() {
            await _client.ConnectAsync(IPAddress.Parse(_ip), _port);
            _stream = _client.GetStream();
        }

        public async Task SendMessage(string message, string userName)
            => await SendMessageAsync(Create(Action.MemberMessage, new MemberMessage(userName, message)), _stream);

        public event Action<string> MessageRecieved;
        public event Action<string> NewMember;
        public event Action<string> MemberDisconnect;
        public event Action<IReadOnlyList<string>> FetchMembers;

        public async Task Listen() {
            using (var reader = new StreamReader(_client.GetStream())) {
                while (true) {
                    var message = await ReadMessageAsync(reader);
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
            }
        }


        public async Task<bool> Register(string userName) {
            await SendMessageAsync(Create(Action.MemberJoin, userName), _stream);
            var data = await new StreamReader(_stream).ReadLineAsync();
            var message = ParseMessage(data);
            if (message.Action == Action.Status) return message.Parse<bool>();
            throw new IOException($"Expected: {Action.Status}, Was:{message.Action}");
        }

        public void CloseConnection() {
            _client.GetStream().Close();
            _client.Close();
        }
    }
}