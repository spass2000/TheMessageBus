using TheMessageBus;
using System.Linq;
using System.Text;

namespace mbDaemon
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args != null && args.Count() > 0 && args[0] == "-?")
            {
                Info.WriteLine("tmbDaemon [-port port] [-http port] [-debug] ");
                System.Environment.Exit(0);
            }

            bool debug = false;
            StringBuilder port = new StringBuilder("6500");
            StringBuilder http = new StringBuilder("6580");
            StringBuilder Value = null;
            int n = 0;
            foreach (var s in args)
            {
                switch (s)
                {
                    case "-debug": debug = true; n = 0; break;
                    case "-port": Value = port; n = 0; break;
                    case "-http": Value = http; n = 0; break;
                    default:
                        switch (n)
                        {
                            case 0:
                                if (Value == null) break;
                                Value.Clear(); Value.Append(s); break;
                        }
                        n++;
                        break;
                }
            }
            Info.SetDebug(debug ? 3 : 1);
            Main m = new Main(int.Parse(port.ToString()), int.Parse(http.ToString()));
            System.Threading.Thread.Sleep(-1);
        }
    }
}