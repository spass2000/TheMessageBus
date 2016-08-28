using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using TheMessageBus;

namespace mbWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Transport trans = null;
        private FaultTolerance FaultTolerance = null;
        private Dictionary<string, Listener> listeners = new Dictionary<string, Listener>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_ClickStop(object sender, RoutedEventArgs e)
        {
            trans?.Close();
            FaultTolerance?.Destroy();
            trans = null;
            FaultTolerance = null;
        }

        private void Button_ClickStart(object sender, RoutedEventArgs e)
        {
            Button_ClickStop(null, null);
            trans = new Transport(int.Parse(this.txtService.Text), this.txtNetwork.Text, this.txtDaemon.Text);

            FaultTolerance = new FaultTolerance(trans, "groupName", 1, null);
            FaultTolerance.FaultToleranceChanged += FaultTolerance_FaultToleranceReceived;
        }

        private void FaultTolerance_FaultToleranceReceived(object Sender, bool Activation)
        {
            Application.Current.Dispatcher.BeginInvoke(
                  DispatcherPriority.Normal,
                  (Action)(() => this.Title += (Activation ? "!" : "#")));

            // this.Title += (Activation?"!":"#"); //  thius.te   MessageBox.Show("FaultTolerance:" + Activation);
        }

        private void Trans_ReliableMessage(object Sender, TheMessageBus.Messages.SubjectInfo Subject, TheMessageBus.Messages.ApplicationInfo Application, Message Data)
        {
            if (Subject.ReceiveSubject != null)
            {
                // trans.Reply();
                // trans.Send(Subject.ReceiveSubject, MessageBus.Convert.GetBytes("RRRRRRRRRRRRRRRRRRRRRRRRRRRR " + DateTime.Now.ToLongTimeString()));
                trans.SendReply(Subject, new Message("RRRRRRRRRRRRRRRRRRRRRRRRRRRR " + DateTime.Now.ToLongTimeString()));
                return;
            }
            MessageBox.Show(Data.ToString());
        }

        private void Button_ClickSubjectAdd(object sender, RoutedEventArgs e)
        {
            // trans.Subscribe(txtSubject.Text);
            Listener l = new Listener(trans, txtSubject.Text);
            listeners.Add(txtSubject.Text, l);
            l.MessageReceived += Trans_ReliableMessage;
        }

        private void Button_ClickSubjectRemove(object sender, RoutedEventArgs e)
        {
            // trans.Unsubscribe(txtSubject.Text);
            Listener l;
            if (listeners.TryGetValue(txtSubject.Text, out l))
            {
                l.MessageReceived -= Trans_ReliableMessage;
                l.Destroy();
                listeners.Remove(txtSubject.Text);
            }
        }

        private void Button_ClickSend(object sender, RoutedEventArgs e)
        {
            trans.Send(txtSubject.Text, new Message(txtData.Text + " " + DateTime.Now.ToLongTimeString()));
        }

        private void Button_ClickSendRequest(object sender, RoutedEventArgs e)
        {
            var ret = trans.SendRequest(txtSubject.Text, new Message(txtData.Text), 10);
            MessageBox.Show(ret.ToString());
        }
    }
}