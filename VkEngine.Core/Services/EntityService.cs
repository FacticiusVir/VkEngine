using System;
using System.Collections.Generic;

namespace VkEngine.Services
{
    public class EntityService
        : GameService, IEntityService
    {
        private readonly Dictionary<Type, EntityManager> managers = new Dictionary<Type, EntityManager>();
        private IUpdateLoopService updateLoop;

        public override void Initialise(Game game)
        {
            base.Initialise(game);

            this.updateLoop = game.Services.GetService<IUpdateLoopService>();
        }

        private void CreateManager(Type type)
        {
            var factory = new EntityFactory();
        }
    }

    public interface IEntityService
        : IGameService
    {
    }
}
