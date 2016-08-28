using System;
using System.Net.Sockets;
using System.Threading;
using static TheMessageBus.TcpTransport.Tools;

namespace TheMessageBus.TcpTransport
{
    // Local transport to mbDaemon
    public class Client
    {

        public delegate void ConnectedEventHandler(object sender);

        public event ConnectedEventHandler Connected;

        public delegate void DisconnectedEventHandler(object sender);

        public event DisconnectedEventHandler Disconnected;

        public delegate void ReceiveEventHandler(object sender, byte[] data);

        public event ReceiveEventHandler Receive;

        private System.Net.Sockets.TcpClient clientSocket = null;

        private int Service;
        private string Daemon;

        private static ManualResetEvent sendDone = new ManualResetEvent(false);

        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        public bool IsConnected { get { if (clientSocket == null) return false; return clientSocket.Connected; } }

        public string ClientInfo()
        {
            return clientSocket.Client.RemoteEndPoint.ToString();
        }

        public Client(int Service, string Daemon)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Daemon)) Daemon = "localhost:6500";
                if (Daemon.StartsWith("localhost")) Tools.StartDeamon();
                this.Service = Service;
                this.Daemon = Daemon;
                StartConnect();
            }
            catch (System.Exception ex)
            {
                Info.Exception("Transport", ex);
            }
        }

        private void StartConnect()
        {
            Info.WriteLine(3, "StartConnect");
            clientSocket = new System.Net.Sockets.TcpClient();
            string[] s = Daemon.Split(':');

            var result = clientSocket.BeginConnect(s[0], int.Parse(s[1]), new AsyncCallback(OnSocketConnected1), clientSocket);
        }

        private string ClientInfo(TcpClient clientSocket)
        {
            return clientSocket.Client.LocalEndPoint.ToString() + " " + clientSocket.Client.RemoteEndPoint.ToString();
        }

        private void OnSocketConnected1(IAsyncResult asynchronousResult)
        {
            try
            {
                // Retrieve the socket from the state object.
                TcpClient client = (TcpClient)asynchronousResult.AsyncState;

                // Complete the connection.
                client.EndConnect(asynchronousResult);

                Info.WriteLine(3, "Socket connected to {0}", ClientInfo(client));
                Connected?.Invoke(this);
                StartReceivingData();
                return;
            }
            catch (System.Net.Sockets.SocketException e)
            {
            }
            catch (System.Exception ex)
            {
                Info.Exception("OnSocketConnected1", ex);
            }
            finally
            {
                // Signal that the connection has been made.
                // connectDone.Set();
            }
            StartConnect();
        }

        public void Send(byte[] b)
        {
            if (!clientSocket.Connected) return;
            // clientSocket.Client.BeginSend(b, 0, b.Length, 0, new AsyncCallback(SendCallback), clientSocket);
            NetworkStream serverStream = clientSocket.GetStream();
            serverStream.Write(b, 0, b.Length);
            serverStream.Flush();
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                TcpClient client = (TcpClient)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.Client.EndSend(ar);
                Info.WriteLine(3, "Sent {0} bytes to server.", bytesSent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
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
                    try
                    {
                        if (Receive != null)
                            Receive(this, by);
                    }
                    catch (System.Exception ex)
                    {
                        Info.Exception("Receive", ex);
                    }
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
            Info.WriteLine("Disconnected");
            receiveDone.Set();
            if (Disconnected != null)
                Disconnected(this);
            StartConnect();
        }

        public void Close()
        {
            Disconnect();
            clientSocket.Close();
        }

        public System.Net.IPEndPoint RemoteEndPoint
        {
            get
            {
                return (System.Net.IPEndPoint)clientSocket.Client.RemoteEndPoint;
            }
        }

        public System.Net.IPEndPoint LocalEndPoint
        {
            get
            {
                return (System.Net.IPEndPoint)clientSocket.Client.LocalEndPoint;
            }
        }
    }
}