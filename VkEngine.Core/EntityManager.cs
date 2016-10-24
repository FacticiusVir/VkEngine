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
        }

        public void AddNew(Array data)
        {
            GCHandle handle = default(GCHandle);

            try
            {
                handle = GCHandle.Alloc(data);

                byte* handlePointer = (byte*)handle.AddrOfPinnedObject().ToPointer();

                for (int index = 0; index < data.Length; index++)
                {
                    IntPtr dataPointer = Marshal.AllocHGlobal((IntPtr)this.bootstrapDataSize);

                    Buffer.MemoryCopy(handlePointer + (this.bootstrapDataSize * index), dataPointer.ToPointer(), this.bootstrapDataSize, this.bootstrapDataSize);
                }
            }
            finally
            {
                handle.Free();
            }
        }

        public void Start(PageWriteKey key)
        {

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
    }
}
