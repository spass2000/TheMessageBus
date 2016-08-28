using System;
using System.Collections.Generic;
using System.Threading;
using TheMessageBus.Messages;

namespace TheMessageBus
{
    public class Transport
    {
        internal delegate void ReliableMessageEventHandler(object Sender, SubjectInfo Subject, ApplicationInfo Application, byte[] Data);

        internal event ReliableMessageEventHandler ReliableMessage;

        public delegate void ConnectedEventHandler(object sender);

        public event ConnectedEventHandler Connected;

        public delegate void DisconnectedEventHandler(object sender);

        public event DisconnectedEventHandler Disconnected;

        private TheMessageBus.TcpTransport.Client trans;
        private int Service;
        private string Network;
        private string Daemon;
        private List<string> Subscriptions;

        private static string MachineName = System.Environment.MachineName;
        private static string UserName = System.Environment.UserName;
        private static string UserDomainName = System.Environment.UserDomainName;
        private static string Application = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        private static Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        private static ManualResetEvent connectDone = new ManualResetEvent(false);

        public Transport(int Service, string Network) : this(Service, Network, "")
        {
        }

        public Transport(int Service, string Network, string Daemon)
        {
            trans = new TcpTransport.Client(Service, Daemon);
            trans.Connected += Trans_Connected;
            trans.Disconnected += Trans_Disconnected;
            trans.Receive += Trans_Receive;
            this.Service = Service;
            this.Network = Network;
            this.Daemon = Daemon;
            Subscriptions = new List<string>();
            connectDone.WaitOne();
        }

        private void Trans_Receive(object sender, byte[] data)
        {
            if (ReliableMessage != null)
            {
                var d = TheMessageBus.Serializer.DeserializeObjectBin<TheMessageBus.Messages.Envelope>(data);
                ReliableMessage(this, d.Subject, d.App, d.Data);
            }
        }

        private void Trans_Disconnected(object sender)
        {
            Info.WriteLine("Disconnected " + trans.ClientInfo());
            Disconnected?.Invoke(this);
        }

        private void Trans_Connected(object sender)
        {
            Info.WriteLine("Connected " + trans.ClientInfo());
            Send(TheMessageBus.Serializer.SerializeObjectBin(new TheMessageBus.Messages.Transport.Create { Service = Service, Network = Network, App = CreateHeader() }));

            foreach (var s in Subscriptions)
                SubscribeSend(s);
            connectDone.Set();
            Connected?.Invoke(this);
        }

        private void L_MessageReceived(object Sender, SubjectInfo Subject, ApplicationInfo Application, byte[] Data)
        {
            throw new NotImplementedException();
        }

        internal void Subscribe(string subject)
        {
            Subscriptions.Add(subject);
            SubscribeSend(subject);
        }

        internal void SubscribeSend(string subject)
        {
            if (!trans.IsConnected) return;
            Send(TheMessageBus.Serializer.SerializeObjectBin(new TheMessageBus.Messages.Subscriptions.Add { Subscription = subject, App = CreateHeader(), Transport = new Messages.TransportInfo { Service = Service, Network = Network } }));
            Info.WriteLine("Listen to " + subject);
        }

        internal void UnsubscribeSend(string subject)
        {
            if (!trans.IsConnected) return;
            Send(TheMessageBus.Serializer.SerializeObjectBin(new TheMessageBus.Messages.Subscriptions.Remove { Subscription = subject, App = CreateHeader(), Transport = new Messages.TransportInfo { Service = Service, Network = Network } }));
            Info.WriteLine("Remove to " + subject);
        }

        public void SendReply(TheMessageBus.Messages.SubjectInfo Subject, Message message)
        {
            Send(Subject.ReceiveSubject, message);
        }

        public void SendReply(TheMessageBus.Messages.SubjectInfo Subject, object request)
        {
            Send(Subject.ReceiveSubject, request);
        }

        internal void Unsubscribe(string subject)
        {
            Subscriptions.Remove(subject);
            UnsubscribeSend(subject);
        }

        internal TheMessageBus.Messages.Envelope SendRequest(string SendSubject, object Data, int Timeout)
        {
            return SendRequest(SendSubject, TheMessageBus.Serializer.SerializeObjectBin(Data), Timeout);
        }

        public Message SendRequest(string SendSubject, Message Message, int Timeout)
        {
            TheMessageBus.Messages.Envelope ret = SendRequest(SendSubject, Message.ToArray(), Timeout);
            if (ret == null) return null;
            return new Message(ret.Data);
        }

        internal TheMessageBus.Messages.Envelope SendRequest(string SendSubject, byte[] Data, int Timeout)
        {
            ManualResetEvent e = new ManualResetEvent(false);
            Messages.Envelope ret = null;
            string ReceiveSubject = "#" + GetUniqueId();
            Listener l = new Listener(this, ReceiveSubject);
            l.MessageReceived += (Sender, Subject, Application, rData) =>
            {
                e.Set();
                ret = new Messages.Envelope { App = Application, Data = rData?.ToArray(), Subject = Subject };
            };

            TheMessageBus.Messages.Envelope env = new TheMessageBus.Messages.Envelope { Transport = new Messages.TransportInfo { Service = Service, Network = Network }, App = CreateHeader(), Data = Data, Subject = new Messages.SubjectInfo { SendSubject = SendSubject, ReceiveSubject = ReceiveSubject }, Timestamp = DateTime.Now };
            Send(TheMessageBus.Serializer.SerializeObjectBin(env));

            e.WaitOne(Timeout * 1000);
            l.Destroy();
            return ret;
        }

        internal void Send(string subject, byte[] message = null)
        {
            TheMessageBus.Messages.Envelope env = new TheMessageBus.Messages.Envelope { Transport = new Messages.TransportInfo { Service = Service, Network = Network }, App = CreateHeader(), Data = message, Subject = new Messages.SubjectInfo { SendSubject = subject }, Timestamp = DateTime.Now };
            Send(TheMessageBus.Serializer.SerializeObjectBin(env));
        }

        internal void Send(string subject, object message = null)
        {
            Send(subject, TheMessageBus.Serializer.SerializeObjectBin(message));
        }

        public void Send(string subject, Message message = null)
        {
            Send(subject, message.ToArray());
        }

        public void Send(string SendSubject, string ReceiveSubject, byte[] message = null)
        {
            TheMessageBus.Messages.Envelope env = new TheMessageBus.Messages.Envelope { Transport = new Messages.TransportInfo { Service = Service, Network = Network }, App = CreateHeader(), Data = message, Subject = new Messages.SubjectInfo { SendSubject = SendSubject, ReceiveSubject = ReceiveSubject }, Timestamp = DateTime.Now };
            Send(TheMessageBus.Serializer.SerializeObjectBin(env));
        }

        private void Send(byte[] v)
        {
            trans.Send(TheMessageBus.LowLevel.SendMessage(v));
        }

        public void Close()
        {
            trans.Close();
        }

        private TheMessageBus.Messages.ApplicationInfo CreateHeader()
        {
            Messages.ApplicationInfo h = new Messages.ApplicationInfo
            {
                Application = Application,
                Version = Version?.ToString(),
                MachineName = MachineName,
                UserName = UserName,
                ClientId = GetClientId()
            };
            return h;
        }

        internal string GetClientId()
        {
            return TcpTransport.Tools.GetLocalIP().Address.ToString("X") + "." + trans.LocalEndPoint.Port.ToString("X");
        }

        private long srcount = 1;

        internal string GetUniqueId()
        {
            return GetClientId() + "." + (srcount++).ToString("X");
        }
    }
}