using System;
using System.Collections.Generic;
using System.Threading;

namespace YoutuBot
{
    public static class C
    {
        public static IYoutubeService Service;
        public static string Now => DateTime.Now.ToString("HH:mm:ss");
        public static Dictionary<int,ConsoleColor> _colors=new Dictionary<int, ConsoleColor>();
        static readonly Random r=new Random();

        private static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(r.Next(v.Length));
        }
        
        public static ConsoleColor Color(int id)
        {
            if (!_colors.ContainsKey(id))  _colors.Add(id, RandomEnumValue<ConsoleColor>());
            return _colors[id];
        }

        public static string Prefix
        {
            get
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var result = Now + ":" + threadId + " :-> ";
                return result;
            }
        }

        static void W(string line)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = Color(threadId);
            Console.Write(Prefix + line);
            Console.ForegroundColor = temp;
        }
        public static void WriteLine(string line)
        {
            W(line+Environment.NewLine);
        }

        public static void WriteLine(string format, params object[] args)
        {
            W(string.Format(format, args)+Environment.NewLine);
        }

        public static void Write(string s)
        {
            W(s);
        }
    }
}