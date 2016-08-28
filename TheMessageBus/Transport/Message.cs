using System.Collections.Generic;

namespace TheMessageBus
{
    public class Message : List<byte>
    {
        public Message(IEnumerable<byte> Bytes) : base(Bytes)
        {
        }

        public Message(string Text) : base(TheMessageBus.Convert.GetBytes(Text))
        {
        }

        public Message(object Data) : base(TheMessageBus.Serializer.SerializeObjectBin(Data))
        {
        }

        public override string ToString()
        {
            return TheMessageBus.Convert.GetString(this.ToArray());
        }

        public T Get<T>()
        {
            return TheMessageBus.Serializer.DeserializeObjectBin<T>(this.ToArray());
        }
    }
}