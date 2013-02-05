using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OETS.Shared.Util
{
    public class Log
    {
        private static Log m_singleton = null;
        private bool logToFile = false;
        private static String fileName = "ALL_" + DateTime.Today.Year + "_" + DateTime.Today.Month + "_" + DateTime.Today.Day + "_" + DateTime.Today.Hour + ".txt";
        private object _cn;

        public object ClassName
        {
            get
            {
                return _cn;
            }
            set
            {
                _cn = value;
            }
        }

        private Log()
        {

        }

        public static Log Instance
        {
            get
            {
                if (m_singleton == null)
                    m_singleton = new Log();

                return m_singleton;
            }
        }

        public void Debug(string message)
        {
#if DEBUG
            Console.WriteLine(message);
#endif
        }

        public void Debug(string message, params object[] arg)
        {
#if DEBUG
            Console.WriteLine(message, arg);
#endif
        }

        public void String(string message)
        {
            Console.WriteLine(message);
        }

        public void String(string message, params object[] args)
        {
            Console.WriteLine(message, args);
            if (logToFile)
            {
                StringBuilder str = new StringBuilder();
                str.AppendFormat(message + "\n", args);
                File.AppendAllText(fileName, str.ToString());
            }
        }

        public void Notice(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(ClassName);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void Notice(string message, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(ClassName);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.Gray;

            if (logToFile)
            {
                StringBuilder str = new StringBuilder();
                str.AppendFormat(message + "\n", args);
                File.AppendAllText(fileName, str.ToString());
            }
        }

        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(ClassName);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("ERROR");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void Error(string message, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(ClassName);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("ERROR");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.Gray;

            if (logToFile)
            {
                StringBuilder str = new StringBuilder();
                str.AppendFormat(message + "\n", args);
                File.AppendAllText(fileName, str.ToString());
            }
        }

        public void Sucess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(ClassName);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;

            if (logToFile)
            {
                StringBuilder str = new StringBuilder();
                str.AppendFormat(message + "\n");
                File.AppendAllText(fileName, str.ToString());
            }
        }

        public void Sucess(string message, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(ClassName);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.Gray;

            if (logToFile)
            {
                StringBuilder str = new StringBuilder();
                str.AppendFormat(message + "\n", args);
                File.AppendAllText(fileName, str.ToString());
            }
        }
    }
}
