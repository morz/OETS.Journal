using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OETS.Shared.Util
{
    public class ProgressBar
    {
        public static void Print(int progress, int total)
        {
            Console.CursorLeft = 0;
            Console.Write("[");
            Console.CursorLeft = 33;
            Console.Write("]");
            Console.CursorLeft = 1;
            float onechunk = 30.0f / total;
            int position = 1;
            double value = 0.0;
            for (int i = 0; i < onechunk * progress; ++i)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.CursorLeft = position++;
                Console.Write(" ");                
            }
            for (int i = position; i <= 32; ++i)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Yellow;
            value = Math.Round((progress / total) * 100.0);
            Console.Write(value.ToString() + "%");
            Console.ForegroundColor = ConsoleColor.Gray;
            if (value >= 99.8f)
                Console.Write("\n");
        }
    }
}
