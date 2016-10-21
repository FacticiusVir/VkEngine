using System;
using System.Collections.Generic;
using System.Linq;

namespace VkEngine
{
    public class Pipeline
    {
        public Pipeline(Delegate pipelineFunction)
        {
            this.Output = pipelineFunction.Method.ReturnType;
            this.Inputs = pipelineFunction.Method.GetParameters().Select(param => param.ParameterType).ToArray();
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

        public Delegate Function
        {
            get;
            private set;
        }
    }
}