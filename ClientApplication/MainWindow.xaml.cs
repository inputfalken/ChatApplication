using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Client;
using static System.Reactive.Linq.Observable;
using static ClientApplication.Helper;

namespace ClientApplication {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly string _userName;

        public MainWindow(ChatClient chatClient, string userName) {
            InitializeComponent();
            _userName = userName;
            FromEventPattern<RoutedEventHandler, RoutedEventArgs>(e => Loaded += e, e => Loaded -= e)
                .Select(_ => chatClient)
                .Subscribe(OnLoaded);
        }

        private void OnLoaded(ChatClient chatClient) {
            var dispatcherScheduler = new DispatcherScheduler(Dispatcher);

            ObserveOnClick(SendBtn)
                .Select(_ => MessageInputBox.Text)
                .Subscribe(async text => await SendMessage(chatClient, text));

            chatClient.MessageRecieved
                .ObserveOn(dispatcherScheduler)
                .Subscribe(message => ChatBox.Items.Add(message));

            chatClient.MemberJoins
                .ObserveOn(dispatcherScheduler)
                .Subscribe(username => Members.Items.Add(username));

            chatClient.MemberDisconnects
                .ObserveOn(dispatcherScheduler)
                .Subscribe(username => Members.Items.Remove(username));

            chatClient.MembersOnline
                .ObserveOn(dispatcherScheduler)
                .Subscribe(userNames => {
                    foreach (var userName in userNames) Members.Items.Add(userName);
                });

            FromEventPattern<EventHandler, EventArgs>(e => Closed += e, e => Closed -= e)
                .Subscribe(_ => chatClient.CloseConnection());

            Task.Run(chatClient.ListenAsync);
        }

        private async Task SendMessage(ChatClient chatClient, string text) {
            string displayMessage;
            if (ChatClient.IsPm(text)) {
                var recipent = ChatClient.ExtractRecipent(text);
                var msg = ChatClient.RemoveRecipent(text, recipent);
                await chatClient.SendPm(msg, _userName, recipent);
                displayMessage = $"PM {recipent}: {msg}";
            }
            else {
                await chatClient.SendChatMessageAsync(text, _userName);
                displayMessage = text;
            }
            ChatBox.Items.Add(displayMessage);
        }
    }
}