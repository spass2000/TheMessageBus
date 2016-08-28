using Polenter.Serialization;
using System.IO;

namespace TheMessageBus
{
    public class Serializer
    {
        private static SharpSerializer serializer = new SharpSerializer(false);

        public static object DeserializeObjectBin(byte[] byteArray)
        {
            return serializer.Deserialize(new MemoryStream(byteArray));
        }

        public static byte[] SerializeObjectBin(object obj, bool prefix = true)
        {
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(obj, stream);
                long len = stream.Position;
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static object DeserializeObjectString(string byteArray)
        {
            return DeserializeObjectBin(Convert.GetBytes(byteArray));
        }

        public static string SerializeObjectString(object obj)
        {
            return Convert.GetString(SerializeObjectBin(obj));
        }

        public static T DeserializeObjectBin<T>(byte[] buffer)
        {
            try
            {
                return (T)DeserializeObjectBin(buffer);
            }
            catch (System.Exception ex)
            {
                return default(T);
            }
        }
    }
}