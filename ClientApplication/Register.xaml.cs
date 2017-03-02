using System;
using System.Windows;
using Client;

namespace ClientApplication {
    /// <summary>
    ///     Interaction logic for Register.xaml
    /// </summary>
    public partial class Register : Window {
        private static readonly ChatClient NetusClient = new ChatClient("10.0.2.15", 23000);

        public Register() {
            InitializeComponent();
            Loaded += OnLoaded;
            RegisterButton.Click += RegisterButtonOnClick;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            try {
                await NetusClient.Connect();
                Label.Content = await NetusClient.ReadMessage();
            }
            catch (Exception) {
                MessageBox.Show("Could not establish an connection to the Server.");
                Close();
            }
        }

        private async void RegisterButtonOnClick(object sender, RoutedEventArgs routedEventArgs) {
            var success = await NetusClient.Register(UserNameBox.Text);
            if (success) {
                var message = await NetusClient.ReadMessage();
                MessageBox.Show(message);
                new MainWindow(NetusClient, UserNameBox.Text).Show();
                Close();
            }
            else {
                MessageBox.Show($"Could register with {UserNameBox.Text}, try with a different name");
            }
        }
    }
}