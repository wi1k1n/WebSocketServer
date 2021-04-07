using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer
{
    class CLI
    {
        private bool listen;
        private Server server;
        private string help = @"Вы используете CLI. Доступны следующие команды:
help\t\tвызов данной справки
clear\t\tочистить буфер консоли
server\t\tполучить информацию о работающем сервере";

        public CLI (Server server)
        {
            this.server = server;
        }

        public void Start ()
        {
            listen = true;
            string read;
            while (listen)
            {
                Console.Write("-->");
                parseCommandLine(Console.ReadLine());
            }
        }

        private void parseCommandLine (string s)
        {
            if (s == "")
                return;
            string[] elts = s.Split(' ');
            switch (elts[0].ToLower())
            {
                case "help":
                    Console.WriteLine(help.Replace(@"\t", "\t"));
                    break;
                case "clear":
                    Console.Clear();
                    break;
                case "server":
                    Console.WriteLine(server);
                    break;
                case "client":
                    if (elts.Length > 1)
                    {

                        int n = -1;
                        if (!int.TryParse(elts[1], out n))
                        {
                            error(1001);
                            break;
                        }
                        if (n >= server.ClientCount)
                        {
                            error(1002, "Клиента с таким индексом не существует!");
                            break;
                        }
                        Console.WriteLine(server.ClientList[n]);
                        break;
                    } else
                    {
                        error(1001);
                        break;
                    }
                    break;
            }
        }
        private void error (int id, string msg)
        {
            string autoMsg = "";
            switch (id)
            {
                case 1001: autoMsg = "Argument not valid.";
                    break;
            }
            Console.WriteLine("!!> Error #{0} occured.{1}{2}", id, autoMsg != "" ? " " + autoMsg : "", msg != "" ? " " + msg : "");
        }
        private void error (int id)
        {
            error(id, "");
        }
    }
}
