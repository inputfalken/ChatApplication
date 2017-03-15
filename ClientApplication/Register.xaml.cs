using System;
using System.Globalization;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using Client;
using static System.Reactive.Linq.Observable;
using static ClientApplication.Helper;

namespace ClientApplication {
    /// <summary>
    ///     Interaction logic for Register.xaml
    /// </summary>
    public partial class Register : Window {
        public Register() {
            InitializeComponent();
            FromEventPattern<RoutedEventHandler, RoutedEventArgs>(e => Loaded += e, e => Loaded -= e)
                .Subscribe(OnLoaded);
        }

        private void OnLoaded(EventPattern<RoutedEventArgs> eventPattern) {
            var dispatcherScheduler = new DispatcherScheduler(Dispatcher);
            var chatClient = new ChatClient();

            ObserveOnClick(ConnectBtn)
                .Where(AddressIsValid)
                .Where(PortIsValid)
                .SelectMany(
                    _ => chatClient.ConnectAsync(new IPEndPoint(IPAddress.Parse(Address.Text), int.Parse(Port.Text))))
                .ObserveOn(dispatcherScheduler)
                .Subscribe(sucessfullConnection => {
                    ConnectBtn.IsEnabled = !sucessfullConnection;
                    DiscoConnect.IsEnabled = sucessfullConnection;
                    Label.Text = sucessfullConnection
                        ? "Successfully connected"
                        : $"Could establish an connection to {Address.Text}:{Port.Text}";
                });

            ObserveOnClick(RegisterBtn)
                .SelectMany(_ => chatClient.RegisterAsync(UserNameBox.Text)) // Consumes the task
                .ObserveOn(dispatcherScheduler) // Use the dispatcher for the following UI updates.
                .Subscribe(successfulRegister => {
                    if (successfulRegister) ProceedToMainWindow(chatClient);
                    else Label.Text = $"{UserNameBox.Text} is taken, try with a different name";
                });
        }

        private bool PortIsValid(EventPattern<RoutedEventArgs> eventPattern) {
            var isInteger = IsInteger(Port.Text);
            if (!isInteger) Label.Text = "Port is invalid";
            return isInteger;
        }

        private bool AddressIsValid(EventPattern<RoutedEventArgs> eventPattern) {
            var isIpAddress = IsIpAddress(Address.Text);
            if (!isIpAddress) Label.Text = "Address is invalid";
            return isIpAddress;
        }

        private void ProceedToMainWindow(ChatClient client) {
            new MainWindow(client, UserNameBox.Text).Show();
            Close();
        }
    }
}