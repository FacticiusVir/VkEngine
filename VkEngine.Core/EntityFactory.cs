using System;
using System.Collections.Generic;

namespace VkEngine
{
    public class EntityFactory
    {
        public IEnumerable<Type> StateTypes
        {
            get;
            set;
        }

        public IEnumerable<Pipeline> Pipelines
        {
            get;
            set;
        }
    }
}
