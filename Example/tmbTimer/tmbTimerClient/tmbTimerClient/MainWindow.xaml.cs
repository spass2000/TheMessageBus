using System;
using System.Windows;
using System.Windows.Threading;
using TheMessageBus;

namespace tmbTimerClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Transport transport;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            transport = new Transport(7701, "224.0.1.24");
            Listener listener = new Listener(transport, "TIMESERVER.EVENT");
            listener.MessageReceived += Listener_MessageReceived;
        }

        private void Listener_MessageReceived(object Sender, TheMessageBus.Messages.SubjectInfo Subject, TheMessageBus.Messages.ApplicationInfo Application, Message Data)
        {
            var msg = Data.Get<Messages.Event>();

            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                 DispatcherPriority.Normal,
                 (Action)(() => this.Title = "TheMessageBus " + msg.Now));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var msg = new Message(new Messages.Command.GetTime.Request { });
            var m = transport.SendRequest("TIMESERVER.COMMAND", msg, 5);
            if (m == null)
            {
                MessageBox.Show("Timeout");
                return;
            }
            var ret = m.Get<Messages.Command.GetTime.Response>();
            MessageBox.Show("Respose from server:" + ret.ErrorCode + " " + ret.ErrorText + " " + ret.Now);
        }
    }
}