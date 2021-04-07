using System;
using System.Threading;

namespace WebSocketServer
{
    class Program
    {
        static int MaximumThreadCount = 4 * Environment.ProcessorCount;
        static int MinimumThreadCount = 2;

        static void Main (string[] args)
        {
            // Стартуем таймер для отсчета времени с начала запуска программы
            Timer.Start();

            // Устанавливаем максимальное и минимальное количество рабочих потоков
            ThreadPool.SetMinThreads(MinimumThreadCount, MinimumThreadCount);
            ThreadPool.SetMaxThreads(MaximumThreadCount, MaximumThreadCount);

            // Создаем экзмепляр сервера и запускаем его в отдельном потоке
            Server server = new Server(8181);
            Thread serverThread = new Thread(delegate() { server.StartServer(); });
            serverThread.Start();

            CLI cli = new CLI(server);
            cli.Start();
        }
    }
}
