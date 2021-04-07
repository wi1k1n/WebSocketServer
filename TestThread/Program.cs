using System;
using System.Threading; //Именно это пространство имен поддерживает многопоточность

namespace ConsoleApplication1
{
    class Program
    {
        static void Main (string[] args)
        {
            Thread myThread = new Thread(func); //Создаем новый объект потока (Thread)

            myThread.Start(); //запускаем поток

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Поток 1 выводит " + i);
                Thread.Sleep(0);
            }

            Console.Read(); //Приостановим основной поток

        }

        //Функция запускаемая из другого потока
        static void func ()
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Поток 2 выводит " + i.ToString());
                Thread.Sleep(0);
            }
        }
    }
}