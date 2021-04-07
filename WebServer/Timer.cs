using System;
using System.Diagnostics;

namespace WebSocketServer
{
    static class Timer
    {
        private static Stopwatch timer = new Stopwatch();
        private static DateTime startDate;

        public static long ElapsedMilliseconds { get { return timer.ElapsedMilliseconds; } }

        public static void Start ()
        {
            startDate = DateTime.Now;
            timer.Start();
        }

        public static void Stop ()
        {
            timer.Stop();
        }
    }
}
