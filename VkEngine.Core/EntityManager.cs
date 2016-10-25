using Sigil;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;

namespace VkEngine
{
    public unsafe class EntityManager
        : IDisposable
    {
        private readonly EntityFactory factory;
        private readonly StatePipeline[] pipelines;
        private readonly Action<IntPtr[]>[] observers;
        private readonly uint bootstrapDataSize;
        private readonly Action<IntPtr, IntPtr[]> bootstrapAction;

        private int count;

        public EntityManager(int pageCount, EntityFactory factory)
        {
            this.factory = factory;
            this.pipelines = factory.StateTypes.Select(type => new StatePipeline
            {
                StateType = type,
                StateTypeSize = (int)MemUtil.SizeOf(type),
                Store = new PagedStore(pageCount, type),
                Pipeline = factory.Pipelines.SingleOrDefault(pipeline => pipeline.Output == type),
                NewObjects = Enumerable.Range(0, pageCount).Select(x => new ConcurrentQueue<IntPtr>()).ToArray()
            }).ToArray();
            this.observers = factory.Pipelines
                                    .Where(pipeline => pipeline.Output == typeof(void))
                                    .Select(this.BuildObserverAction)
                                    .ToArray();
            this.bootstrapDataSize = MemUtil.SizeOf(this.factory.BootstrapType);

            this.bootstrapAction = this.BuildBootstrapAction();
        }

        private Action<IntPtr[]> BuildObserverAction(Pipeline observerPipeline)
        {
            var observerEmitter = Emit<Action<IntPtr[]>>.NewDynamicMethod();

            var stateLocals = this.pipelines.Select(statePipeline => observerEmitter.DeclareLocal(statePipeline.StateType))
                                            .ToArray();

            int index = 0;

            foreach (var stateLocal in stateLocals)
            {
                var readFromPtrInfo = typeof(MemUtil).GetMethod("ReadFromPtr")
                                                        .MakeGenericMethod(stateLocal.LocalType);

                observerEmitter.LoadArgument(0)
                                .LoadConstant(index)
                                .LoadElement<IntPtr>()
                                .LoadLocalAddress(stateLocal)
                                .Call(readFromPtrInfo);

                index++;
            }

            foreach (var stateLocal in stateLocals)
            {
                observerEmitter.LoadLocal(stateLocal);
            }

            return observerEmitter.Call(observerPipeline.Function)
                                    .Return()
                                    .CreateDelegate();
        }

        private Action<IntPtr, IntPtr[]> BuildBootstrapAction()
        {
            var readFromPtrInfo = typeof(MemUtil).GetMethod("ReadFromPtr").MakeGenericMethod(this.factory.BootstrapType);

            var bootstrapEmitter = Emit<Action<IntPtr, IntPtr[]>>.NewDynamicMethod();

            Local dataLocal = bootstrapEmitter.DeclareLocal(this.factory.BootstrapType);
            var resultLocals = this.factory.Bootstrap.GetParameters()
                                                        .Skip(1)
                                                        .Select(param => bootstrapEmitter.DeclareLocal(param.ParameterType.GetElementType()))
                                                        .ToArray();

            bootstrapEmitter.LoadArgument(0)
                            .LoadLocalAddress(dataLocal)
                            .Call(readFromPtrInfo)
                            .LoadLocal(dataLocal);

            foreach (var resultLocal in resultLocals)
            {
                bootstrapEmitter.LoadLocalAddress(resultLocal);
            }

            bootstrapEmitter.Call(this.factory.Bootstrap);

            int localIndex = 0;

            foreach (var resultLocal in resultLocals)
            {
                var writeToPtrInfo = typeof(MemUtil).GetMethods()
                                                    .Single(x => x.Name == "WriteToPtr" && x.GetParameters().Length == 2)
                                                    .MakeGenericMethod(resultLocal.LocalType);

                bootstrapEmitter.LoadArgument(1);
                bootstrapEmitter.LoadConstant(localIndex);
                bootstrapEmitter.LoadElement<IntPtr>();
                bootstrapEmitter.LoadLocal(resultLocal);
                bootstrapEmitter.Call(writeToPtrInfo);

                localIndex++;
            }

            return bootstrapEmitter.Return().CreateDelegate();
        }

        public void Start(PageWriteKey key, Array data)
        {
            int scaledCapacity = FindScaledCapacity(data.Length);

            for (int index = 0; index < this.pipelines.Length; index++)
            {
                this.pipelines[index].Store.UpdateWriteCapacity(key, scaledCapacity);
            }

            GCHandle dataHandle = default(GCHandle);

            try
            {
                dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

                IntPtr handlePointer = dataHandle.AddrOfPinnedObject();

                for (int index = 0; index < data.Length; index++)
                {
                    this.BootstrapEntity(key, index, handlePointer + (int)(this.bootstrapDataSize * index));
                }

                this.count = data.Length;
            }
            finally
            {
                if (dataHandle.IsAllocated)
                {
                    dataHandle.Free();
                }
            }
        }

        private void BootstrapEntity(PageWriteKey key, int entityId, IntPtr dataPointer)
        {
            var resultArray = new IntPtr[this.pipelines.Length];

            for (int pipelineIndex = 0; pipelineIndex < this.pipelines.Length; pipelineIndex++)
            {
                resultArray[pipelineIndex] = this.pipelines[pipelineIndex].Store.GetWritePage(key) + (this.pipelines[pipelineIndex].StateTypeSize * entityId);
            }

            this.bootstrapAction(dataPointer, resultArray);
        }

        public void Update(PageWriteKey key)
        {
            var stateArray = new IntPtr[this.pipelines.Length];

            for (int pipelineIndex = 0; pipelineIndex < this.pipelines.Length; pipelineIndex++)
            {
                stateArray[pipelineIndex] = this.pipelines[pipelineIndex].Store.GetReadPage(key);
            }

            for (int entityIndex = 0; entityIndex < this.count; entityIndex++)
            {
                for (int observerIndex = 0; observerIndex < this.observers.Length; observerIndex++)
                {
                    this.observers[observerIndex](stateArray);
                }

                for (int pipelineIndex = 0; pipelineIndex < this.pipelines.Length; pipelineIndex++)
                {
                    stateArray[pipelineIndex] += this.pipelines[pipelineIndex].StateTypeSize;
                }
            }
        }

        public void Dispose()
        {
            foreach (var pipeline in this.pipelines)
            {
                pipeline.Store.Dispose();
            }
        }

        private struct StatePipeline
        {
            public Type StateType;
            public int StateTypeSize;
            public PagedStore Store;
            public Pipeline Pipeline;
            public ConcurrentQueue<IntPtr>[] NewObjects;
        }

        private static int FindScaledCapacity(int minimumCapacity)
        {
            int padding = minimumCapacity / 10;
            padding = Math.Max(padding, 16);

            return 1 << (int)Math.Ceiling(Math.Log(minimumCapacity + padding, 2));
        }
    }
}
