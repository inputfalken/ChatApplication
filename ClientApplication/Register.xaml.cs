﻿using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private async void OnLoaded(EventPattern<RoutedEventArgs> eventPattern) {
            var dispatcherScheduler = new DispatcherScheduler(Dispatcher);
            var chatClient = new ChatClient("10.0.2.15", 23000);
            try {
                await chatClient.Connect();
            }
            catch (Exception) {
                MessageBox.Show("Could not establish an connection to the Server.");
                Close();
            }
            ObserveOnClick(RegisterBtn)
                .SelectMany(_ => chatClient.RegisterAsync(UserNameBox.Text)) // Consumes the task
                .ObserveOn(dispatcherScheduler) // Use the dispatcher for the following UI updates.
                .Subscribe(successfulRegister => {
                    if (successfulRegister) ProceedToMainWindow(chatClient);
                    else Label.Content = $"{UserNameBox.Text} is taken, try with a different name";
                });
        }

        private void ProceedToMainWindow(ChatClient client) {
            new MainWindow(client, UserNameBox.Text).Show();
            Close();
        }
    }
}