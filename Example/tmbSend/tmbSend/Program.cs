using TheMessageBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbSend
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Count() == 0 || args[0] == "-?")
            {
                Info.WriteLine("tmbSend [-service service] [-network network] [-daemon daemon] [-debug] <subject> <messages>");
                System.Environment.Exit(0);
            }

            bool debug = false;
            StringBuilder Service = new StringBuilder();
            StringBuilder Daemon = new StringBuilder();
            StringBuilder Network = new StringBuilder();
            StringBuilder Port = new StringBuilder();
            string Subject = string.Empty;
            string Message = string.Empty;
            StringBuilder Value = null;
            int n = 0;
            foreach (var s in args)
            {
                switch (s)
                {
                    case "-debug": debug = true; ; n = 0; break;
                    case "-service": Value = Service; n = 0; break;
                    case "-network": Value = Network; n = 0; break;
                    case "-daemon": Value = Daemon; n = 0; break;
                    case "-port": Value = Port; n = 0; break;
                    default:
                        switch (n)
                        {

                            case 0: Value.Append(s); break;
                            case 1: Subject = s; break;
                            case 2: Message = s; break;
                        }
                        n++;
                        break;
                }
            }
            Info.SetDebug(debug ? 3 : 1);
            Transport trans = new Transport(GetInt(Service.ToString()), Network.ToString(), Daemon.ToString());
            Console.WriteLine("tmbSend Service:" + Service + " Network:" + Network + " Daemon:" + Daemon);
            trans.Send(Subject, new TheMessageBus.Message(Message + DateTime.Now.ToLongTimeString()));
            // System.Threading.Thread.Sleep(500);
            trans.Close();

        }

        private static int GetInt(string v)
        {
            int i;
            if (int.TryParse(v, out i)) return i;
            return 0;
        }
    }
}
