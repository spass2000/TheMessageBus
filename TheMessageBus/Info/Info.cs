using System;

namespace TheMessageBus
{
    public class Info
    {
        private static int level = 0; // 0,1,2

        public static void WriteLine(string s)
        {
            Output(s);
        }

        public static void WriteLine(int Level, string s)
        {
            Output(Level, s);
        }

        public static void WriteLine()
        {
            Console.WriteLine();
        }

        private static void Output(int Level, string s)
        {
            if (Level > level) return;
            Output(s);
        }

        private static void Output(string s)
        {
            Console.WriteLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff") + " " + s);
        }

        public static void Exception(string s, Exception se)
        {
            Output("### " + s + " " + se);
        }

        public static void SetDebug(int flag)
        {
            level = flag;
        }

        public static void WriteLine(int Level, string format, params object[] arg)
        {
            if (Level > level) return;
            Console.WriteLine(format, arg);
        }
    }
}