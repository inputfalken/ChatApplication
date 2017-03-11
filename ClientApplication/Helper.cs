using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ClientApplication {
    public static class Helper {
        internal static IObservable<EventPattern<RoutedEventArgs>> ObserveOnClick(ButtonBase btn)
            => Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                eventhandler => btn.Click += eventhandler,
                eventhandler => btn.Click -= eventhandler
            );
    }
}