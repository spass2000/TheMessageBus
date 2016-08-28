using System.Text;

namespace TheMessageBus
{
    internal class Convert
    {
        static public string GetString(byte[] buffer, int read)
        {
            return Encoding.Default.GetString(buffer, 0, read);
        }

        static public string GetString(byte[] buffer)
        {
            if (buffer == null) return null;
            return Encoding.Default.GetString(buffer);
        }

        static public byte[] GetBytes(string byteArray)
        {
            return Encoding.Default.GetBytes(byteArray);
        }
    }
}