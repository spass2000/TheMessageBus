using TheMessageBus;
using TheMessageBus.Messages;
using TheMessageBus.TcpTransport;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace mbDaemon
{
    public class Main
    {
        static public DateTime DateTime = DateTime.Now;

        public class DaemonTransport
        {
            public Multicast Multicast;
            public Unicast Unicast;
        }

        public class Client
        {
            public class Transport
            {
                public DaemonTransport transport;                
                public Dictionary<string, List<string>> Subscriptions = new Dictionary<string, List<string>>();
            }

            public Dictionary<int, Transport> Transports = new Dictionary<int, Transport>();
            public ApplicationInfo ApplicationInfo;
            public Request Request;
        }

        private TheMessageBus.TcpTransport.Server LocalTransport;
        public static Dictionary<int, DaemonTransport> Transports = new Dictionary<int, DaemonTransport>();

        public static  Dictionary<Request, Client> Clients = new Dictionary<Request, Client>();
        private NancyHost _nancy;
        int ServerPort;
        public Main(int port, int http)
        {
            Info.WriteLine("TheMessageBus - tmbDaemon startet...");
            InfoOutput();
            ServerPort = port;

            LocalTransport = new Server(ServerPort);
            Info.WriteLine("Localport:" + ServerPort);
            LocalTransport.Connected += LocalTransport_Connected;
            LocalTransport.Disconnected += LocalTransport_Disconnected;
            LocalTransport.Receive += LocalTransport_Receive;

            var config = new HostConfiguration();
            config.RewriteLocalhost = false;

            string url = "http://localhost:" + http;
            Info.WriteLine(url);
            _nancy = new NancyHost(new MyBootstrapper(), config, new Uri(url));
            _nancy.Start();
        }

        private void LocalTransport_Disconnected(object sender)
        {
            Clients.Remove((Request)sender);
        }

        private void LocalTransport_Connected(object sender)
        {
            Request r = sender as Request;
            Clients.Add(r, new Client { Request = r });
        }

        private void Transport_Receive(object sender, byte[] data)
        {
            var msg = TheMessageBus.Serializer.DeserializeObjectBin<TheMessageBus.Messages.Base>(data);

            switch (msg.Command)
            {
                case "Envelope":
                    {
                        var m = TheMessageBus.Serializer.DeserializeObjectBin<TheMessageBus.Messages.Envelope>(data);
                        var subject = m.Subject.SendSubject.Split('.');
                        var u = TheMessageBus.LowLevel.SendMessage(data);

                        Info.WriteLine(3, "Message " + m.Subject.SendSubject + "/" + m.Subject.ReceiveSubject);

                        foreach (var rr in Clients.Values.ToList().Where(s => s.Transports.Any(i => i.Key == m.Transport.Service)))
                        {
                            try
                            {
                                if (rr.Transports.Where(i => i.Key == m.Transport.Service && CheckSubscriptions(subject, i.Value.Subscriptions.Values.ToList())).Count() > 0)
                                {
                                    rr.Request.Send(u);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Info.Exception("Transport_ReliableMessage", ex);
                            }
                        }
                    }
                    break;

                default: Info.Exception("Command not found " + msg.Command, null); break;
            }
        }

        private void LocalTransport_Receive(object sender, byte[] data)
        {
            Request r = sender as Request;
            Client cl;
            if (!Clients.TryGetValue(r, out cl)) return;

            var msg = TheMessageBus.Serializer.DeserializeObjectBin<TheMessageBus.Messages.Base>(data);

            switch (msg.Command)
            {
                case "Transport+Create":
                    {
                        var m = TheMessageBus.Serializer.DeserializeObjectBin<TheMessageBus.Messages.Transport.Create>(data);
                        cl.ApplicationInfo = m.App;
                        lock (this)
                        {
                            DaemonTransport transport;
                            if (!Transports.TryGetValue(m.Service, out transport))
                            {
                                Info.WriteLine(3, "Create Transport:" + m.Service + " " + m.Network);
                                transport = new DaemonTransport();
                                transport.Multicast = new Multicast(m.Service, m.Network);
                                transport.Unicast = new Unicast(ServerPort + 1);

                                Transports.Add(m.Service, transport);
                                transport.Multicast.Receive += Transport_Receive;
                                transport.Unicast.Receive += Transport_Receive;
                            }
                            else
                            {
                                if (m.Network != transport.Multicast.MulticastIp)
                                {
                                    Info.WriteLine(3, "### Error Transport:" + m.Service + " " + m.Network);
                                    return;
                                }
                                Info.WriteLine(3, "Reuse Transport:" + m.Service + " " + m.Network);
                            }
                            Client.Transport tr;
                            if (!cl.Transports.TryGetValue(m.Service, out tr))
                            {
                                tr = new Client.Transport();
                                cl.Transports.Add(m.Service, tr);
                            }
                            tr.transport = transport;
                        }
                    }
                    break;

                case "Envelope":
                    {
                        var m = TheMessageBus.Serializer.DeserializeObjectBin<TheMessageBus.Messages.Envelope>(data);
                        var subject = m.Subject.SendSubject.Split('.');
                        var u = TheMessageBus.LowLevel.SendMessage(data);

                        DaemonTransport transport;
                        if (Transports.TryGetValue(m.Transport.Service, out transport))
                        {
                            if (subject[0][0] == '#')
                            {
                                IPAddress Ip = Tools.GetIPAddress(subject[0].Substring(1));
                                transport.Unicast.Send(Ip,u);
                            }
                            else transport.Multicast.Send(u);
                        }
                    }
                    break;

                case "Subscriptions+Add":
                    {
                        var m = TheMessageBus.Serializer.DeserializeObjectBin<TheMessageBus.Messages.Subscriptions.Add>(data);
                        Info.WriteLine(3, "Subscriptions.Add: " + m.Subscription);
                        Client.Transport tr;
                        if (cl.Transports.TryGetValue(m.Transport.Service, out tr))
                        {
                            if (!tr.Subscriptions.ContainsKey(m.Subscription))
                            tr.Subscriptions.Add(m.Subscription,m.Subscription.Split('.').ToList());
                        }
                    }
                    break;
                case "Subscriptions+Remove":
                    {
                        var m = TheMessageBus.Serializer.DeserializeObjectBin<TheMessageBus.Messages.Subscriptions.Remove>(data);
                        Info.WriteLine(3, "Subscriptions.Remove: " + m.Subscription);
                        Client.Transport tr;
                        if (cl.Transports.TryGetValue(m.Transport.Service, out tr))
                        {
                            tr.Subscriptions.Remove(m.Subscription);
                        }
                    }
                    break;

                default:
                    {
                        Info.Exception("Command not found " + msg.Command, null);
                        break;
                    }
            }
        }

        

        private bool CheckTransport(int service, Dictionary<int, Multicast> clientTransport)
        {
            Multicast t;
            return clientTransport.TryGetValue(service, out t);
        }

        private bool CheckSubscriptions(string[] subjectpart, List<List<string>> subscriptions)
        {
            if (subscriptions.Count() == 0) return false;
            int n = -1;
            foreach (var s in subjectpart)
            {
                n++;
                if (!subscriptions.Any(u => u.Count()>n && ( u[n] == s || u[n] == "*")))
                {
                    Info.WriteLine(3, "Ignore Subject:<" + GetSubject(subjectpart) + ">");
                    foreach (var q in subscriptions)
                        Info.WriteLine(3, GetSubject(q.ToArray()));
                    return false;
                }
            }
            return true;
        }

        private string GetSubject(string[] s)
        {
            string ret = "";
            foreach (var u in s)
                ret += u + ".";
            return ret;
        }

        private void InfoOutput()
        {
            Info.WriteLine("MachineName:" + System.Environment.MachineName);
            Info.WriteLine("UserName:   " + System.Environment.UserName);
            Info.WriteLine("DomainName: " + System.Environment.UserDomainName);

            Info.WriteLine();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    Info.WriteLine("Hostname IP address: " + ip.MapToIPv4()); 
            }
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            IPInterfaceProperties properties = adapter.GetIPProperties();
                            Info.WriteLine("Detected IP interface: " + ip.Address + " (" + adapter.Description + ")");
                        }
                    }
            }
        }
    }
}