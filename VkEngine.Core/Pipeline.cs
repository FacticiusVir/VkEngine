using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VkEngine
{
    public class Pipeline
    {
        public Pipeline(MethodInfo pipelineFunction)
        {
            this.Output = pipelineFunction.ReturnType;
            this.Inputs = pipelineFunction.GetParameters().Select(param => param.ParameterType).ToArray();
            this.Function = pipelineFunction;
        }

        public Type Output
        {
            get;
            private set;
        }

        public IEnumerable<Type> Inputs
        {
            get;
            private set;
        }

        public MethodInfo Function
        {
            get;
            private set;
        }
    }
}