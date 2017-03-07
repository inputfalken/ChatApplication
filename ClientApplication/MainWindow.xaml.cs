using System;
using System.Collections.Generic;
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
            _netusClient.FetchMembers += NetusClientOnFetchMembers;
            _netusClient.MemberDisconnect += NetusClientOnMemberDisconnect;
            _userName = userName;
            SendBtn.Click += SendBtnOnClick;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }


        private async void NetusClientOnFetchMembers(IReadOnlyList<string> readOnlyList) {
            await Dispatcher.InvokeAsync(() => {
                foreach (var username in readOnlyList) Members.Items.Add(username);
            });
        }

        private async void NetusClientOnMemberDisconnect(string s)
            => await Dispatcher.InvokeAsync(() => Members.Items.Remove(s));

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

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs) => Task.Run(_netusClient.Listen);


        private void AddToChatBox(string message) => ChatBox.Items.Add(message);
    }
}