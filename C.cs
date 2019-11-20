using System;
using System.Collections.Generic;
using System.Threading;

namespace YoutuBot
{
    public static class C
    {
        public static IYoutubeService Service;
        public static Dictionary<int, ConsoleColor> _colors = new Dictionary<int, ConsoleColor>();
        private static readonly Random r = new Random();
        public static string Now => DateTime.Now.ToString("HH:mm:ss");

        public static string Prefix
        {
            get
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var result = Now + ":" + threadId + " :-> ";
                return result;
            }
        }

        private static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T) v.GetValue(r.Next(v.Length));
        }
        public static object sync=new object();
        public static ConsoleColor Color(int id)
        {
            lock (sync)
            {
                if (!_colors.ContainsKey(id)) _colors.Add(id, RandomEnumValue<ConsoleColor>());
                return _colors[id];
            }
        }

        private static void W(string line)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = Color(threadId);
            Console.Write(Prefix + line);
            Console.ForegroundColor = temp;
        }

        public static void WriteLine(string line)
        {
            W(line + Environment.NewLine);
        }

        public static void WriteLine(string format, params object[] args)
        {
            W(string.Format(format, args) + Environment.NewLine);
        }

        public static void Write(string s)
        {
            W(s);
        }
    }
}