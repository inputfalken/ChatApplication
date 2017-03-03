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
        private readonly string _userName;

        public MainWindow(ChatClient netusClient, string userName) {
            InitializeComponent();
            _netusClient = netusClient;
            _netusClient.MessageRecieved += NetusClientOnMessageRecieved;
            _netusClient.NewMember += NetusClientOnNewMember;
            _userName = userName;
            SendBtn.Click += SendBtnOnClick;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private async void NetusClientOnNewMember(string s) => await Dispatcher.InvokeAsync(() => Members.Items.Add(s));

        private async void NetusClientOnMessageRecieved(string s) => await Dispatcher.InvokeAsync(() => AddToChatBox(s));

        private void OnClosed(object sender, EventArgs eventArgs) {
            _netusClient.CloseConnection();
        }


        private async void SendBtnOnClick(object sender, RoutedEventArgs routedEventArgs) {
            var messageBoxText = MessageInputBox.Text;
            //Server needs to send back the message with a timestamp for when message was recieved.
            await _netusClient.SendMessage(messageBoxText, _userName);
            AddToChatBox($"{_userName}: {messageBoxText}");
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            await ListenAsync();
            //when closing in debug mode you'll get an exception about an disposed networkstream.
        }

        private async Task ListenAsync() {
            while (true) {
                await Task.Run(_netusClient.Listen);
                //TODO Message needs to be handled differently. Message could contain anything from the protocol!
            }
        }

        private void AddToChatBox(string message) => ChatBox.Items.Add(message);
    }
}