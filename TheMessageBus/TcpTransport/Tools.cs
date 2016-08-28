using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TheMessageBus.TcpTransport
{
    public class Tools
    {
        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;

            // Size of receive buffer.
            public const int BufferSize = 25600;

            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];

            // Received data string.
            public List<byte> data = new List<byte>();

            internal void Add(int bytesRead)
            {
                byte[] d = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, d, 0, bytesRead);
                data.AddRange(d);
            }

            internal void Remove(int len)
            {
                data.RemoveRange(0, len + 10);
            }
        }

        static public IPAddress GetLocalIP()
        {
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address;
                        }
                    }
                }
            }
            return null;
        }

        public static void StartDeamon()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.FileName = "tmbDaemon.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                System.Diagnostics.Process.Start(startInfo);
                System.Threading.Thread.Sleep(500);
            }
            catch (System.Exception ex)
            {
            }
        }

        public static IPAddress GetIPAddress(string v)
        {
            long l = long.Parse(v, System.Globalization.NumberStyles.HexNumber);
            IPAddress i = new IPAddress(l);
            return i;
        }
    }
}