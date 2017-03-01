using System;
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
            var message = JAction.ParseToMessage(await new StreamReader(_client.GetStream()).ReadLineAsync());
            return $"{message.Sender}: {message.Result}";
        }

        public async Task SendMessage(string message) {
            var buffer = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            await _client.GetStream().WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task<bool> Register(string userName) {
            await SendMessage(JAction.MemberJoins(userName).ToString());
            var toJAction = JAction.ParseToJAction(await new StreamReader(_client.GetStream()).ReadLineAsync());
            if (toJAction.Action != JAction.StatusAction)
                throw new IOException($"Expected Action :{JAction.StatusAction}, was: {toJAction.Action}");
            return toJAction.Result == JAction.Success;
        }

        public void CloseConnection() {
            _client.GetStream().Close();
            _client.Close();
        }
    }
}