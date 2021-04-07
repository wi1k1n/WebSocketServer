using System;
using System.Threading;

namespace WebSocketServer
{
    static class SendTest
    {
        public static void Start(WSServer srv)
        {
            int x = 0, y = 0;
            double ang = 0;
            while(true)
            {
                Tick(ref x, ref y, ref ang);
                srv.Broadcast(x + ";" + y);
                Thread.Sleep(100);
            }
        }
        private static void Tick(ref int x, ref int y, ref double ang)
        {
            Random rnd = new Random();
            ang += rnd.Next(-15, 15) * Math.PI / 180;
            ang %= Math.PI * 2;
            double d = 2;
            x += (int)(Math.Cos(ang) * d);
            y += (int)(Math.Sin(ang) * d);
        }
    }
}
