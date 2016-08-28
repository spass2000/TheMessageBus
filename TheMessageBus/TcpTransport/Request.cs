using System;
using System.Net;
using System.Net.Sockets;
using static TheMessageBus.TcpTransport.Tools;

namespace TheMessageBus.TcpTransport
{
    public class Request
    {
        private TcpClient clientSocket;
        private Server Server;
        private int port;

        public Request(Server Server, TcpClient clientConnected)
        {
            port = ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Port;
            this.clientSocket = clientConnected;
            this.Server = Server;
            Server.ConnectedEvent(this);
            Info.WriteLine("Connected " + ClientInfo());
        }

        private string ClientInfo()
        {
            return clientSocket.Client.RemoteEndPoint.ToString();
        }

        public void StartClient()
        {
            StartReceivingData();
        }

        public void Send(byte[] b)
        {
            if (!clientSocket.Connected) return;
            NetworkStream serverStream = clientSocket.GetStream();
            serverStream.Write(b, 0, b.Length);
            serverStream.Flush();
        }

        private void StartReceivingData()
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = clientSocket.Client;

                // Begin receiving the data from the remote device.
                clientSocket.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
                    Disconnect();
                    return;
                }

                state.Add(bytesRead);

                int count;
                int len;
                byte[] by;
                while ((by = TheMessageBus.LowLevel.ConvertMessage(state.data, out count, out len)) != null)
                {
                    state.data.RemoveRange(0, len);
                    Server.ReceiveEvent(this, by);
                }

                // Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Disconnect();
            }
            catch (Exception e)
            {
                Info.Exception("ReceiveCallback", e);
                Disconnect();
            }
        }

        private void Disconnect()
        {
            Info.WriteLine("Disconnected " + ClientInfo());
            Server.DisonnectedEvent(this);
        }
    }
}