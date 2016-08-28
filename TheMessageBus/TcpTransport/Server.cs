using System;
using System.Net;
using System.Net.Sockets;

namespace TheMessageBus.TcpTransport
{
    public class Server
    {
        public delegate void ConnectedEventHandler(object sender);

        public event ConnectedEventHandler Connected;

        public delegate void DisconnectedEventHandler(object sender);

        public event DisconnectedEventHandler Disconnected;

        public delegate void ReceiveEventHandler(object sender, byte[] data);

        public event ReceiveEventHandler Receive;

        private TcpListener _tcpListener;

        public void DisonnectedEvent(Request handleClientRequest)
        {
            if (Disconnected != null)
                Disconnected(handleClientRequest);
        }

        public void ConnectedEvent(Request handleClientRequest)
        {
            if (Connected != null)
                Connected(handleClientRequest);
        }

        public void ReceiveEvent(Request handleClientRequest, byte[] data)
        {
            if (Receive != null)
                Receive(handleClientRequest, data);
        }

        public Server(int ServerPort)
        {
            try
            {
                _tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, ServerPort));
                _tcpListener.Start();
                _tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(OnClientConnect), _tcpListener);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                System.Environment.Exit(1);
            }
            catch (System.Exception ex)
            {
                Info.Exception("Server", ex);
                System.Environment.Exit(1);
            }
        }

        private void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                TcpClient clientSocket = default(TcpClient);
                clientSocket = _tcpListener.EndAcceptTcpClient(asyn);
                Request clientReq = new Request(this, clientSocket);
                clientReq.StartClient();
            }
            catch (Exception se)
            {
                Info.Exception("OnClientConnect", se);
                throw;
            }

            WaitForClientConnect();
        }

        private void WaitForClientConnect()
        {
            _tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(OnClientConnect), _tcpListener);
        }
    }
}