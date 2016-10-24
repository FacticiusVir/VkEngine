using Sigil;
using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using VkEngine.Model;

namespace VkEngine
{
    class Program
    {
        static unsafe void Main(string[] args)
        {
            BindingFlags publicStaticFlags = BindingFlags.Public | BindingFlags.Static;

            var factory = new EntityFactory()
            {
                BootstrapType = typeof(Vector2),
                Bootstrap = typeof(Program).GetMethod("Bootstrap", publicStaticFlags),
                Pipelines = new Pipeline[] { },
                StateTypes = new[] { typeof(Transform2) }
            };
            var manager = new EntityManager(3, factory);

            manager.AddNew(new[] { new Vector2(1, 1) });

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

        }
    }
}
