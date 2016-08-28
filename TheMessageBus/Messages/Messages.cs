using System;
using System.Linq;

namespace TheMessageBus.Messages
{
    public class Base
    {
        public Base()
        {
            string s = this.GetType().FullName.Split('.').Last();
            Command = s;
        }

        public string Command { get; set; }
    }

    public class ApplicationInfo
    {
        public string ClientId { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public string Application { get; set; }
        public string Version { get; set; }
    }

    public class SubjectInfo
    {
        public string SendSubject { get; set; }
        public string ReceiveSubject { get; set; }
    }

    public class TransportInfo
    {
        public int Service { get; set; }
        public string Network { get; set; }
    }

    public class Envelope : Base
    {
        public DateTime Timestamp { get; set; }
        public SubjectInfo Subject { get; set; }
        public TransportInfo Transport { get; set; }
        public ApplicationInfo App { get; set; }

        public byte[] Data { get; set; }
    }

    public class Transport
    {
        public class Create : Base
        {
            public int Service { get; set; }
            public string Network { get; set; }
            public ApplicationInfo App { get; set; }
        }
    }

    public class Subscriptions
    {
        public class Add : Base
        {
            public TransportInfo Transport { get; set; }
            public string Subscription { get; set; }
            public ApplicationInfo App { get; set; }
        }

        public class Remove : Base
        {
            public TransportInfo Transport { get; set; }
            public string Subscription { get; set; }
            public ApplicationInfo App { get; set; }
        }
    }

    public class FaultTolerance
    {
        public class Start : Base
        {
            public string ClientId { get; set; }
        }

        public class Heartbeat : Base
        {
            public string ClientId { get; set; }
        }

        public class Election : Base
        {
            public string ClientId { get; set; }
        }
    }
}