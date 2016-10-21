using System;

namespace VkEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            var manager = new PageManager(3);

            for (int loop = 0; loop < 20; loop++)
            {
                PageWriteKey key = manager.GetWriteKey();

                Console.WriteLine($"Read Page: {key.ReadPage}");
                Console.WriteLine($"Write Page: {key.WritePage}");
                Console.WriteLine();

                manager.Release(key);
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
