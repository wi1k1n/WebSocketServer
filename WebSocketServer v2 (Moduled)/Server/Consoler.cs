using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer
{
    static class Consoler
    {
        public static void Write (object s, ConsoleColor fgClr, ConsoleColor bgClr, params object[] pars)
        {
            return;
            ConsoleColor fgOldClr = Console.ForegroundColor;
            ConsoleColor bgOldClr = Console.BackgroundColor;
            Console.ForegroundColor = fgClr;
            Console.BackgroundColor = bgClr;

            if (s == null)
                Console.Write("");
            else
                if (pars == null)
                    Console.Write(s.ToString());
                else
                    Console.Write(s.ToString(), pars);
            
            Console.ForegroundColor = fgOldClr;
            Console.BackgroundColor = bgOldClr;
        }
        public static void Write (object s, ConsoleColor fgClr, params object[] pars)
        {
            Write(s, fgClr, Console.BackgroundColor, pars);
        }
        public static void Write (object s, params object[] pars)
        {
            Write(s, Console.ForegroundColor, pars);
        }
        public static void Write (object s)
        {
            Write(s, null);
        }

        public static void WriteLine (object s, ConsoleColor fgClr, ConsoleColor bgClr, params object[] pars)
        {
            Write(string.Concat(s.ToString(), Environment.NewLine), fgClr, bgClr, pars);
        }
        public static void WriteLine (object s, ConsoleColor fgClr, params object[] pars)
        {
            Write(string.Concat(s.ToString(), Environment.NewLine), fgClr, Console.BackgroundColor, pars);
        }
        public static void WriteLine (object s, params object[] pars)
        {
            Write(string.Concat(s.ToString(), Environment.NewLine), Console.ForegroundColor, pars);
        }
        public static void WriteLine (object s)
        {
            Write(string.Concat(s.ToString(), Environment.NewLine), null);
        }
        public static void WriteLine ()
        {
            Write(Environment.NewLine);
        }
    }
}
