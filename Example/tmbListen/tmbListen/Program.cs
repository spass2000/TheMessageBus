using TheMessageBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbListen
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Count() == 0 || args[0] == "-?")
            {
                Info.WriteLine("tmbListen [-service service] [-network network] [-daemon daemon] [-debug] subject_list");
                System.Environment.Exit(0);
            }
            bool debug = false;
            StringBuilder Service = new StringBuilder();
            StringBuilder Daemon = new StringBuilder();
            StringBuilder Network = new StringBuilder();
            string Subject = string.Empty;
            StringBuilder Value = null;
            int n = 0;
            foreach (var s in args)
            {
                switch (s.ToLower())
                {
                    case "-debug": debug = true; ; n = 0; break;
                    case "-service": Value = Service; n = 0; break;
                    case "-network": Value = Network; n = 0; break;
                    case "-daemon": Value = Daemon; n = 0; break;
                    default:
                        switch (n)
                        {
                            case 0: if (Value != null) Value.Append(s); break;
                            case 1: Subject = s; break;
                        }
                        n++;
                        break;
                }
            }
            Info.SetDebug(debug ? 3 : 1);
            Transport trans = new Transport(GetInt(Service.ToString()), Network.ToString(), Daemon.ToString());
            Console.WriteLine("tmbListen Service:" + Service + " Network:" + Network + " Daemon:" + Daemon);

            Listener listener = new Listener(trans, Subject);
            listener.MessageReceived += Trans_MessageReceived;

            System.Threading.Thread.Sleep(-1);

        }

        private static void Trans_MessageReceived(object sender, TheMessageBus.Messages.SubjectInfo subject, TheMessageBus.Messages.ApplicationInfo app, Message  data)
        {            
            Info.WriteLine("MessageReceived:" + app.MachineName + " " + data);
        }

        private static int GetInt(string v)
        {
            int i;
            if (int.TryParse(v, out i)) return i;
            return 0;
        }

      
    }
}
