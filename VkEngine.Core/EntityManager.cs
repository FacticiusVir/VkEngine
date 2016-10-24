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
        private readonly Pipeline[] observers;
        private readonly uint bootstrapDataSize;
        private readonly Action<IntPtr> bootstrapAction;

        public EntityManager(int pageCount, EntityFactory factory)
        {
            this.factory = factory;
            this.pipelines = factory.StateTypes.Select(type => new StatePipeline
            {
                StateType = type,
                Store = new PagedStore(pageCount, type),
                Pipeline = factory.Pipelines.SingleOrDefault(pipeline => pipeline.Output == type),
                NewObjects = Enumerable.Range(0, pageCount).Select(x => new ConcurrentQueue<IntPtr>()).ToArray()
            }).ToArray();
            this.observers = factory.Pipelines.Where(pipeline => pipeline.Output == typeof(void)).ToArray();
            this.bootstrapDataSize = MemUtil.SizeOf(this.factory.BootstrapType);

            var readFromPtrInfo = typeof(MemUtil).GetMethod("ReadFromPtr").MakeGenericMethod(this.factory.BootstrapType);

            var bootstrapEmitter = Emit<Action<IntPtr>>.NewDynamicMethod();

            Local dataLocal = bootstrapEmitter.DeclareLocal(this.factory.BootstrapType);
            var resultLocals = this.factory.Bootstrap.GetParameters()
                                                        .Skip(1)
                                                        .Select(param => bootstrapEmitter.DeclareLocal(param.ParameterType.GetElementType()))
                                                        .ToArray();

            bootstrapEmitter.LoadArgument(0)
                            .LoadLocalAddress(dataLocal)
                            .Call(readFromPtrInfo)
                            .LoadLocal(dataLocal);

            foreach(var resultLocal in resultLocals)
            {
                bootstrapEmitter.LoadLocalAddress(resultLocal);
            }

            bootstrapEmitter.Call(this.factory.Bootstrap)
                            .Return();

            this.bootstrapAction = bootstrapEmitter.CreateDelegate();
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
                    this.bootstrapAction(handlePointer + (int)(this.bootstrapDataSize * index));
                }
            }
            finally
            {
                if(dataHandle.IsAllocated)
                {
                    dataHandle.Free();
                }
            }
        }

        public void Update()
        {

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
