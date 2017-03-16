using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Protocol;
using static Protocol.Message;
using Action = Protocol.Action;

namespace Client {
    public class ChatClient {
        private readonly TcpClient _client;
        private Stream _stream;


        public ChatClient() {
            _client = new TcpClient();
        }

        public ISubject<string> MessageRecieved { get; } = new Subject<string>();
        public ISubject<IReadOnlyList<string>> MembersOnline { get; } = new Subject<IReadOnlyList<string>>();
        public ISubject<string> MemberJoins { get; } = new Subject<string>();
        public ISubject<string> MemberDisconnects { get; } = new Subject<string>();

        public async Task<bool> TryConnectAsync(IPEndPoint ipEnd) {
            try {
                await _client.ConnectAsync(ipEnd.Address, ipEnd.Port);
                _stream = _client.GetStream();
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        public async Task RequestChat() => await SendMessageAsync(Create(Action.Chat, true), _stream);

        public async Task SendChatMessageAsync(string message, string userName)
            => await SendMessageAsync(Create(Action.ChatMessage, new ChatMessage(userName, message)), _stream);

        public async Task ListenAsync() {
            using (var reader = new StreamReader(_client.GetStream())) {
                //TODO Tell server that client is ready for a chat session. It's the reason to why some clients are not synced.
                while (true) {
                    var message = await ReadMessageAsync(reader);
                    switch (message.Action) {
                        case Action.MemberJoin:
                            MemberJoins.OnNext(message.Parse<string>());
                            break;
                        case Action.SendMembers:
                            MembersOnline.OnNext(message.Parse<IReadOnlyList<string>>());
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
                            MemberDisconnects.OnNext(message.Parse<string>());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }


        public async Task<bool> RegisterAsync(string userName) {
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