using System;

namespace YoutuBot
{
    public static class C
    {
        public static string Now => DateTime.Now.ToString("HH:mm:ss");
        public static void WriteLine(string line)
        {
            Console.WriteLine(Now + " -> " + line);
        }

        public static void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(Now + ":" + format, args);
        }
    }
}