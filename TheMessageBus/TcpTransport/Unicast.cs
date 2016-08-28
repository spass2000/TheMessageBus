using System;
using System.Net;
using System.Net.Sockets;
using static TheMessageBus.TcpTransport.Tools;

namespace TheMessageBus.TcpTransport
{
    public class Unicast
    {
        public delegate void ReceiveEventHandler(object sender, byte[] data);

        public event ReceiveEventHandler Receive;

        private UdpClient listener;
        private int Port;
        private IPEndPoint groupEP;

        public Unicast(int Port)
        {
            try
            {
                this.Port = Port;
                listener = new UdpClient(); //  Port);
                listener.ExclusiveAddressUse = false;
                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Client.Bind(new IPEndPoint(IPAddress.Any, Port));

                groupEP = new IPEndPoint(IPAddress.Any, Port);

                StateObject state = new StateObject();
                state.workSocket = listener.Client;
                listener.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (System.Exception ex)
            {
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
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
                    state.data.RemoveRange(0, len);
                    Receive?.Invoke(this, by);
                }

                // Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                // Disconnect();
            }
            catch (Exception e)
            {
                Info.Exception("ReceiveCallback", e);
                // Disconnect();
            }
        }

        public void Send(IPAddress IPAddress, byte[] Data)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ep = new IPEndPoint(IPAddress, Port);
            s.SendTo(Data, ep);
            s.Close();
        }
    }
}