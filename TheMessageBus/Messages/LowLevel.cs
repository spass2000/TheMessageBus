using System;
using System.Collections.Generic;

namespace TheMessageBus
{
    public class LowLevel
    {
        private static byte[] Prefix = new byte[] { 6, 88 };

        public static byte[] ConvertMessage(List<byte> message, out int count, out int len)
        {
            count = len = 0;
            if (message == null || message.Count == 0) return null;
            return ConvertMessage(message.ToArray(), out count, out len);
        }

        public static byte[] ConvertMessage(byte[] message, out int count, out int len)
        {
            count = len = 0;
            if (Prefix[0] != message[0] || Prefix[1] != message[1] || message.Length <= 11) return null;
            if (!CheckMessage(message)) return null;
            count = BitConverter.ToInt16(message, 2);
            len = BitConverter.ToInt16(message, 6);
            if (len + 10 > message.Length) return null;
            //  Console.WriteLine("### MC Receive " + count);
            byte[] b = new byte[len];
            Buffer.BlockCopy(message, 10, b, 0, len);
            len += 10;
            return b;
        }

        public static bool CheckMessage(byte[] message)
        {
            if (Prefix[0] != message[0] || Prefix[1] != message[1] || message.Length <= 11) return false;
            return true;
        }

        public static byte[] SendMessage(byte[] outStream)
        {
            var n1 = BitConverter.GetBytes(0);
            var n2 = BitConverter.GetBytes(outStream.Length);
            byte[] b = new byte[Prefix.Length + outStream.Length + n1.Length + n2.Length];

            Buffer.BlockCopy(Prefix, 0, b, 0, Prefix.Length);
            Buffer.BlockCopy(n1, 0, b, Prefix.Length, n1.Length);
            Buffer.BlockCopy(n2, 0, b, Prefix.Length + n1.Length, n2.Length);
            Buffer.BlockCopy(outStream, 0, b, Prefix.Length + n1.Length + n2.Length, outStream.Length);
            return b;
        }
    }
}