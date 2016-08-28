using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using TheMessageBus;

namespace tmbTimerServer
{
    class Program
    {
        static TheMessageBus.Transport transport;
        static System.Timers.Timer timer;
        static bool Activated = false;
        static void Main(string[] args)
        {
            // Create Multicast Transport for the Communication
            transport = new TheMessageBus.Transport(7701, "224.3.0.5","");


            FaultTolerance FaultTolerance = new FaultTolerance(transport, "MyTimeServerGroup", 1);
            FaultTolerance.FaultToleranceChanged += FaultTolerance_FaultToleranceReceived;

            // Create Listener and add Subject
            Listener Listener = new Listener(transport, "TIMESERVER.COMMAND");
            Listener.MessageReceived += Listener_MessageReceived;

            // Create Timer for sending Events
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            
            System.Threading.Thread.Sleep(-1);
        }

        private static void FaultTolerance_FaultToleranceReceived(object Sender, bool Activation)
        {
            Console.WriteLine("FaultTolerance switched to " + Activation);
            Activated = Activation;
            if (Activation) timer.Start(); else timer.Stop();
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Send Event " + DateTime.Now);
            transport.Send("TIMESERVER.EVENT", new Message(new Messages.Event { Now = DateTime.Now }));
        }

        private static void Listener_MessageReceived(object Sender, TheMessageBus.Messages.SubjectInfo Subject, TheMessageBus.Messages.ApplicationInfo Application, Message Data)
        {
            // This Server is not active...
            if (!Activated) return;

            var m = Data.Get<Messages.Command.Base.Request>();

            switch (m.Command)
            {
                case "GetTime":
                    var inmsg = Data.Get<Messages.Command.GetTime.Request>();
                    transport.SendReply(Subject, new Message(new Messages.Command.GetTime.Response{ Now= DateTime.Now }));
                    break;
                 default:
                    transport.SendReply(Subject, new Message(new Messages.Command.Base.Response {  ErrorCode=-999, ErrorText="Command not found!"}));
                    break;
            }
        }
    }
}
