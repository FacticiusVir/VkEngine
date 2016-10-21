using System;

namespace VkEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(1 << (int)Math.Ceiling(Math.Log(17, 2)));

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
