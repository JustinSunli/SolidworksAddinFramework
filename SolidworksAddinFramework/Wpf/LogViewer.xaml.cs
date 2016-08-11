﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ReactiveUI;
using ReactiveUI.Events;
using Weingartner.ReactiveCompositeCollections;
using static LanguageExt.Prelude;

namespace SolidworksAddinFramework.Wpf
{
    /// <summary>
    /// A simple class for creating a log viewer. Call the static Log message.
    /// </summary>
    public partial class LogViewer
    {
        public CompositeSourceList<LogEntry> LogEntries { get; set; }

        public LogViewer()
        {
            InitializeComponent();
            LogEntries = new CompositeSourceList<LogEntry>();

            ClearButton.Events().MouseUp
                .Subscribe(_ => ClearAll());

            var filteredEntries =
                LogEntries.Where
                    (this.WhenAnyValue(p => p.FilterText.Text).Select(str =>fun((LogEntry v) => v.Message.Contains(str))))
                    .CreateObservableCollection(EqualityComparer<LogEntry>.Default);

            MainPanel.DataContext = filteredEntries;

        }

        private static Lazy<LogViewer> Window = new Lazy<LogViewer>(() => CreateLogViewer().Result); 

        public static void Log(LogEntry entry)
        {
            var window = Window.Value;
            window.Dispatcher.Invoke(()=> window.LogEntries.Add(entry));
        }

        public void ClearAll()
        {
            LogEntries.RemoveRange(LogEntries.Source);
        }

        public static void Invoke(Action a)
        {
            Window.Value.Dispatcher.Invoke(a);
        }

        public static Task<LogViewer> CreateLogViewer()
        {
            var tcs = new TaskCompletionSource<LogViewer>();
            var thread = new Thread(() =>
            {
                // Set up the SynchronizationContext so that any awaits
                // resume on the STA thread as they would in a GUI app.
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
                var viewer = new LogViewer();
                viewer.Show();
                tcs.SetResult(viewer);
                Dispatcher.Run();
            });
            thread.Name = "LoggerThread";

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        private static int CurrentIndex = 0;

        public static void LogWithIndent(int i, string message)
        {
            Log(new string(' ',i*4) + message);

            
        }
        public static void Log(string message)
        {
            try
            {
                Log(new LogEntry() {Message = message,DateTime = DateTime.Now,Index = CurrentIndex++});
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

    }
}