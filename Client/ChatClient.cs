using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
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
            => await SendMessageAsync(Create(Action.ChatMessage, new ChatMessage(userName, message)), _stream);

        public Subject<string> MessageRecieved { get; } = new Subject<string>();
        public Subject<IReadOnlyList<string>> FetchMembers { get; } = new Subject<IReadOnlyList<string>>();
        public Subject<string> NewMember { get; } = new Subject<string>();
        public Subject<string> MemberDisconnected { get; } = new Subject<string>();

        public async Task Listen() {
            using (var reader = new StreamReader(_client.GetStream())) {
                //TODO Tell server that client is ready for a chat session. It's the reason to why some clients are not synced.
                while (true) {
                    var message = await ReadMessageAsync(reader);
                    switch (message.Action) {
                        case Action.MemberJoin:
                            NewMember.OnNext(message.Parse<string>());
                            break;
                        case Action.SendMembers:
                            FetchMembers.OnNext(message.Parse<IReadOnlyList<string>>());
                            break;
                        case Action.ChatMessage:
                            var memberMessage = message.Parse<ChatMessage>();
                            MessageRecieved.OnNext($"{memberMessage.UserName}: {memberMessage.Message}");
                            break;
                        case Action.Message:
                            break;
                        case Action.Status:
                            break;
                        case Action.MemberDisconnect:
                            MemberDisconnected.OnNext(message.Parse<string>());
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