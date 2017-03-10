using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Client;
using static System.Reactive.Linq.Observable;

namespace ClientApplication {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly string _userName;

        public MainWindow(ChatClient netusClient, string userName) {
            InitializeComponent();
            _userName = userName;
            FromEventPattern(this, "Loaded")
                .Select(_ => netusClient)
                .Subscribe(OnLoaded);
        }

        private void OnLoaded(ChatClient chatClient) {
            var dispatcherScheduler = new DispatcherScheduler(Dispatcher);

            FromEventPattern(SendBtn, "Click")
                .Select(_ => MessageInputBox.Text)
                .Subscribe(async message => {
                    AddToChatBox(message);
                    await chatClient.SendMessage(message, _userName);
                });

            chatClient.MessageRecieved
                .ObserveOn(dispatcherScheduler)
                .Subscribe(s => ChatBox.Items.Add(s));

            chatClient.NewMember
                .ObserveOn(dispatcherScheduler)
                .Subscribe(s => Members.Items.Add(s));

            chatClient.MemberDisconnected
                .ObserveOn(dispatcherScheduler)
                .Subscribe(s => Members.Items.Remove(s));

            chatClient.FetchMembers
                .ObserveOn(dispatcherScheduler)
                .Subscribe(userNames => {
                    foreach (var userName in userNames) Members.Items.Add(userName);
                });

            FromEventPattern(this, "Closed")
                .Subscribe(_ => chatClient.CloseConnection());

            Task.Run(chatClient.Listen);
        }


        private void AddToChatBox(string message) => ChatBox.Items.Add(message);
    }
}