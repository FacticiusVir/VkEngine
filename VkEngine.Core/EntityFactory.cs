using System;
using System.Collections.Generic;
using System.Reflection;

namespace VkEngine
{
    public class EntityFactory
    {
        public Type BootstrapType
        {
            get;
            set;
        }

        public MethodInfo Bootstrap
        {
            get;
            set;
        }

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
