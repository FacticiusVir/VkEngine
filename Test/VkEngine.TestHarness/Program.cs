using System;
using System.Numerics;
using System.Reflection;
using VkEngine.Model;

namespace VkEngine
{
    class Program
    {
        static unsafe void Main(string[] args)
        {
            var factory = new EntityFactory()
            {
                BootstrapType = typeof(Vector2),
                Bootstrap = typeof(Program).GetMethod("Bootstrap"),
                Pipelines = new[]
                {
                    new Pipeline(typeof(Program).GetMethod("Display"))
                },
                StateTypes = new[] { typeof(Transform2) }
            };
            var manager = new EntityManager(3, factory);

            manager.Start(new PageWriteKey(0, 1), new[] { new Vector2(1, 1), new Vector2(32, 24) });

            manager.Update(new PageWriteKey(1, 2));

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void Bootstrap(Vector2 position, out Transform2 transform)
        {
            transform = new Transform2
            {
                Position = position
            };
        }

        public static void Display(Transform2 transform)
        {
            Console.WriteLine($"Position: {transform.Position}");
        }
    }
}
