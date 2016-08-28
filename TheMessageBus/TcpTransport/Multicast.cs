using System;
using System.Net;
using System.Net.Sockets;
using static TheMessageBus.TcpTransport.Tools;

namespace TheMessageBus.TcpTransport
{
    public class Multicast
    {
        public delegate void ReceiveEventHandler(object sender, byte[] data);

        public event ReceiveEventHandler Receive;

        private UdpClient udpClientMulticast;
        private IPEndPoint MulticastEndPoint;
        private int ServicePort;
        public string MulticastIp;

        private int MulticastReceive = 0;
        private int MulticastSend = 0;

        public Multicast(int Service, string Multicast)
        {
            this.ServicePort = Service;
            this.MulticastIp = Multicast;

            MulticastEndPoint = new IPEndPoint(IPAddress.Parse(Multicast), Service);

            udpClientMulticast = new UdpClient(); //  Service);
            udpClientMulticast.ExclusiveAddressUse = false;
            udpClientMulticast.JoinMulticastGroup(IPAddress.Parse(Multicast));
            udpClientMulticast.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            var MulticastEndPoint2 = new IPEndPoint(IPAddress.Any, Service);

            udpClientMulticast.Client.Bind(MulticastEndPoint2);

            StateObject multistate = new StateObject();
            multistate.workSocket = udpClientMulticast.Client;
            udpClientMulticast.Client.BeginReceive(multistate.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnUdpDataMulticast), multistate);
        }

        private void OnUdpDataMulticast(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);

            if (bytesRead == 0)
            {
                System.Diagnostics.Debugger.Break();
                return;
            }

            state.Add(bytesRead);

            int count;
            int len;
            byte[] by;
            while ((by = TheMessageBus.LowLevel.ConvertMessage(state.data, out count, out len)) != null)
            {
                MulticastReceive++;
                state.data.RemoveRange(0, len);
                Receive?.Invoke(this, by);
            }
            udpClientMulticast.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnUdpDataMulticast), state);
        }

        public void Send(byte[] b)
        {
            if (!TheMessageBus.LowLevel.CheckMessage(b))
            {
                System.Diagnostics.Debugger.Break();
                return;
            }
            Info.WriteLine(3, "### MC send " + MulticastSend);
            udpClientMulticast.Send(b, b.Length, MulticastEndPoint);
            MulticastSend++;
        }
    }
}