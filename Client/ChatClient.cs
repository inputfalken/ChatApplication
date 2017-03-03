using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Protocol;

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
            var message = JAction.ParseMessage(await new StreamReader(_client.GetStream()).ReadLineAsync());
            return $"{message.Sender}: {message.Result}";
        }

        public async Task SendMessage(string message, string userName) {
            await MessageAsync(JAction.Message(message, userName));
        }

        private async Task MessageAsync(string message) {
            var buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            await _client.GetStream().WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task<bool> Register(string userName) {
            await MessageAsync(JAction.MemberJoins(userName));
            var data = await new StreamReader(_client.GetStream()).ReadLineAsync();
            var action = JAction.ParseJAction(data);
            if (action.Action == JAction.MessageAction) {
                var parseMessage = JAction.ParseMessage(data);
                var status = JAction.ParseJAction(parseMessage.Result);
                if (status.Action == JAction.StatusAction) return status.Result == JAction.Success;
                throw new IOException(
                    $"Expected Action: {JAction.StatusAction}, {parseMessage.Sender} sent: {status.Action}");
            }
            throw new IOException($"Expected Action {JAction.MessageAction}, was: {action.Action}");
        }

        public void CloseConnection() {
            _client.GetStream().Close();
            _client.Close();
        }
    }
}