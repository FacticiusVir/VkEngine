using System;
using System.Numerics;
using System.Runtime.InteropServices;
using VkEngine.Model;
using VkEngine.Services;

namespace VkEngine
{
    class Program
    {
        static unsafe void Main(string[] args)
        {
            //var game = new Game();

            //game.BindService<IEntityService, EntityService>();

            //game.Initialise();

            //game.Start();

            //game.SignalStop();

            //game.Stop();

            var factory = new EntityFactory()
            {
                BootstrapType = typeof(Vector2),
                Bootstrap = typeof(Program).GetMethod("Bootstrap"),
                Pipelines = new[]
                {
                    new Pipeline(typeof(Program).GetMethod("Display")),
                    new Pipeline(typeof(Program).GetMethod("Gravity")),
                    new Pipeline(typeof(Program).GetMethod("Update"))
                },
                StateTypes = new[] { typeof(Vector2), typeof(Transform2) }
            };

            var manager = new EntityManager(3, factory);
            var pageManager = new PageManager(3);

            PageWriteKey key = pageManager.GetWriteKey();

            manager.StartUpdate(key);

            var data = new[] { new Vector2(1, 1)};

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            manager.Add(key, handle.AddrOfPinnedObject());

            handle.Free();

            manager.Update(key);

            pageManager.Release(key);

            key = pageManager.GetWriteKey();

            manager.StartUpdate(key);
            manager.Update(key);
            Console.WriteLine();

            pageManager.Release(key);

            key = pageManager.GetWriteKey();

            manager.StartUpdate(key);
            manager.Update(key);
            Console.WriteLine();

            pageManager.Release(key);

            key = pageManager.GetWriteKey();

            manager.StartUpdate(key);
            manager.Update(key);
            Console.WriteLine();

            pageManager.Release(key);

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void Bootstrap(Vector2 position, out Transform2 transform, out Vector2 velocity)
        {
            transform = new Transform2
            {
                Position = position
            };

            velocity = new Vector2(1, 1);
        }

        public static Transform2 Update(Transform2 transform, Vector2 velocity)
        {
            transform.Position += velocity;

            return transform;
        }

        public static Vector2 Gravity(Vector2 velocity)
        {
            velocity -= new Vector2(0, 1);

            return velocity;
        }

        public static void Display(Transform2 transform, Vector2 velocity)
        {
            Console.WriteLine($"Position: {transform.Position}");
            Console.WriteLine($"Velocity: {velocity}");
            Console.WriteLine();
        }
    }
}
