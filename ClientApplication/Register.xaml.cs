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
            }
            catch (Exception) {
                MessageBox.Show("Could not establish an connection to the Server.");
                Close();
            }
        }

        private async void RegisterButtonOnClick(object sender, RoutedEventArgs routedEventArgs) {
            var success = await NetusClient.Register(UserNameBox.Text);
            if (success) {
                new MainWindow(NetusClient, UserNameBox.Text).Show();
                Close();
            }
            else {
                Label.Content = $"{UserNameBox.Text} is taken, try with a different name";
            }
        }
    }
}