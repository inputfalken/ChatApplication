using System;
using System.Threading.Tasks;
using System.Windows;
using Client;

namespace ClientApplication {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly ChatClient _netusClient;

        public MainWindow(ChatClient netusClient) {
            InitializeComponent();
            _netusClient = netusClient;
            SendBtn.Click += SendBtnOnClick;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnClosed(object sender, EventArgs eventArgs) {
            _netusClient.CloseConnection();
        }


        private async void SendBtnOnClick(object sender, RoutedEventArgs routedEventArgs) {
            var messageBoxText = MessageInputBox.Text;
            //Server needs to send back the message with a timestamp for when message was recieved.
            await _netusClient.SendMessage(messageBoxText);
            AddToChatBox(messageBoxText);
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            await ListenAsync();
            //when closing in debug mode you'll get an exception about an disposed networkstream.
        }

        private async Task ListenAsync() {
            while (true) {
                var message = await Task.Run(_netusClient.ReadMessage);
                //TODO Message needs to be handled differently. Message could contain anything from the protocol!
                AddToChatBox(message);
            }
        }

        private void AddToChatBox(string message) => ChatBox.Items.Add(message);
    }
}