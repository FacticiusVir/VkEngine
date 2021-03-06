﻿using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VkEngine
{
    public unsafe class EntityManager
        : IDisposable, IUpdatable
    {
        private readonly EntityFactory factory;
        private readonly StatePipeline[] pipelines;
        private readonly Action<IntPtr[]>[] observers;
        private readonly uint bootstrapDataSize;
        private readonly Dictionary<Type, int> stateTypeIndices;
        private readonly Action<IntPtr, IntPtr[]> bootstrapAction;

        private readonly object countLock = new object();
        private readonly PagedProperty<int> count;
        private readonly PagedProperty<int> capacity;

        public EntityManager(int pageCount, EntityFactory factory)
        {
            this.factory = factory;
            this.count = new PagedProperty<int>(pageCount);
            this.capacity = new PagedProperty<int>(pageCount);

            this.stateTypeIndices = factory.StateTypes.Select((x, y) => Tuple.Create(x, y))
                                                        .ToDictionary(x => x.Item1, x => x.Item2);

            this.pipelines = factory.StateTypes.Select(type => new StatePipeline
            {
                StateType = type,
                StateTypeSize = (int)MemUtil.SizeOf(type),
                Store = new PagedStore(pageCount, type),
                Pipeline = this.BuildStatePipeline(factory.Pipelines.SingleOrDefault(pipeline => pipeline.Output == type))
            }).ToArray();
            this.observers = factory.Pipelines
                                    .Where(pipeline => pipeline.Output == typeof(void))
                                    .Select(this.BuildObserverAction)
                                    .ToArray();
            this.bootstrapDataSize = MemUtil.SizeOf(this.factory.BootstrapType);

            this.bootstrapAction = this.BuildBootstrapAction();
        }

        private Action<IntPtr, IntPtr[]> BuildStatePipeline(Pipeline statePipeline)
        {
            if (statePipeline == null)
            {
                return null;
            }

            var pipelineEmitter = Emit<Action<IntPtr, IntPtr[]>>.NewDynamicMethod();

            var writeLocal = pipelineEmitter.DeclareLocal(statePipeline.Output);
            var readLocals = statePipeline.Inputs
                                            .Select(input => pipelineEmitter.DeclareLocal(input))
                                            .ToArray();

            foreach (var stateLocal in readLocals)
            {
                var readFromPtrInfo = typeof(MemUtil).GetMethod("ReadFromPtr")
                                                        .MakeGenericMethod(stateLocal.LocalType);

                pipelineEmitter.LoadArgument(1)
                                .LoadConstant(this.stateTypeIndices[stateLocal.LocalType])
                                .LoadElement<IntPtr>()
                                .LoadLocalAddress(stateLocal)
                                .Call(readFromPtrInfo);
            }

            foreach (var stateLocal in readLocals)
            {
                pipelineEmitter.LoadLocal(stateLocal);
            }

            var writeToPtrInfo = typeof(MemUtil).GetMethods()
                                                .Single(x => x.Name == "WriteToPtr" && x.GetParameters().Length == 2)
                                                .MakeGenericMethod(statePipeline.Output);

            pipelineEmitter.Call(statePipeline.Function)
                            .StoreLocal(writeLocal)
                            .LoadArgument(0)
                            .LoadLocal(writeLocal)
                            .Call(writeToPtrInfo);

            return pipelineEmitter.Return().CreateDelegate();
        }

        private Action<IntPtr[]> BuildObserverAction(Pipeline observerPipeline)
        {
            var observerEmitter = Emit<Action<IntPtr[]>>.NewDynamicMethod();

            var stateLocals = observerPipeline.Inputs
                                                .Select(input => observerEmitter.DeclareLocal(input))
                                                .ToArray();

            foreach (var stateLocal in stateLocals)
            {
                var readFromPtrInfo = typeof(MemUtil).GetMethod("ReadFromPtr")
                                                        .MakeGenericMethod(stateLocal.LocalType);

                observerEmitter.LoadArgument(0)
                                .LoadConstant(this.stateTypeIndices[stateLocal.LocalType])
                                .LoadElement<IntPtr>()
                                .LoadLocalAddress(stateLocal)
                                .Call(readFromPtrInfo);
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
            var resultTypes = this.factory.Bootstrap.GetParameters()
                                                        .Skip(1)
                                                        .Select(x => x.ParameterType.GetElementType())
                                                        .ToArray();
            var stateLocals = this.factory.StateTypes
                                            .Select(type => bootstrapEmitter.DeclareLocal(type))
                                            .ToArray();

            bootstrapEmitter.LoadArgument(0)
                            .LoadLocalAddress(dataLocal)
                            .Call(readFromPtrInfo)
                            .LoadLocal(dataLocal);

            foreach (var local in resultTypes.Select(x => stateLocals.Single(y => x == y.LocalType)))
            {
                bootstrapEmitter.LoadLocalAddress(local);
            }

            bootstrapEmitter.Call(this.factory.Bootstrap);

            foreach (var local in stateLocals)
            {
                var writeToPtrInfo = typeof(MemUtil).GetMethods()
                                                    .Single(x => x.Name == "WriteToPtr" && x.GetParameters().Length == 2)
                                                    .MakeGenericMethod(local.LocalType);

                bootstrapEmitter.LoadArgument(1);
                bootstrapEmitter.LoadConstant(this.stateTypeIndices[local.LocalType]);
                bootstrapEmitter.LoadElement<IntPtr>();
                bootstrapEmitter.LoadLocal(local);
                bootstrapEmitter.Call(writeToPtrInfo);
            }

            return bootstrapEmitter.Return().CreateDelegate();
        }

        public int Add(PageWriteKey key, IntPtr data)
        {
            int entityID;

            lock (this.countLock)
            {
                entityID = this.count.GetNew(key);

                int newCount = entityID + 1;

                if (newCount > this.capacity.GetNew(key))
                {
                    //TODO Handle overflow of capacity with queue

                    throw new Exception("Too many entities created per frame.");
                }

                this.count.Set(key, newCount);
            }

            this.BootstrapEntity(key, entityID, data);

            return entityID;
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

        public void StartUpdate(PageWriteKey key)
        {
            int previousCount = this.count.Copy(key);
            int newCapacity = FindScaledCapacity(previousCount);
            this.capacity.Set(key, newCapacity);

            for (int pipelineIndex = 0; pipelineIndex < this.pipelines.Length; pipelineIndex++)
            {
                this.pipelines[pipelineIndex].Store.UpdateWriteCapacity(key, newCapacity);
            }
        }

        public void Update(PageWriteKey key)
        {
            var readArray = new IntPtr[this.pipelines.Length];
            var writeArray = new IntPtr[this.pipelines.Length];

            int previousCount;
            lock (this.countLock)
            {
                previousCount = this.count.Get(key);
            }

            for (int pipelineIndex = 0; pipelineIndex < this.pipelines.Length; pipelineIndex++)
            {
                readArray[pipelineIndex] = this.pipelines[pipelineIndex].Store.GetReadPage(key);
                writeArray[pipelineIndex] = this.pipelines[pipelineIndex].Store.GetWritePage(key);
            }

            for (int entityIndex = 0; entityIndex < previousCount; entityIndex++)
            {
                for (int observerIndex = 0; observerIndex < this.observers.Length; observerIndex++)
                {
                    this.observers[observerIndex](readArray);
                }

                for (int pipelineIndex = 0; pipelineIndex < this.pipelines.Length; pipelineIndex++)
                {
                    var pipeline = this.pipelines[pipelineIndex].Pipeline;

                    if (pipeline != null)
                    {
                        pipeline(writeArray[pipelineIndex], readArray);
                    }
                    else
                    {
                        int stepSize = this.pipelines[pipelineIndex].StateTypeSize;

                        Buffer.MemoryCopy(readArray[pipelineIndex].ToPointer(), writeArray[pipelineIndex].ToPointer(), stepSize, stepSize);
                    }
                }

                for (int pipelineIndex = 0; pipelineIndex < this.pipelines.Length; pipelineIndex++)
                {
                    int stepSize = this.pipelines[pipelineIndex].StateTypeSize;

                    readArray[pipelineIndex] += stepSize;
                    writeArray[pipelineIndex] += stepSize;
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

        private static int FindScaledCapacity(int minimumCapacity)
        {
            int padding = minimumCapacity / 10;
            padding = Math.Max(padding, 16);

            return 1 << (int)Math.Ceiling(Math.Log(minimumCapacity + padding, 2));
        }

        private struct StatePipeline
        {
            public Type StateType;
            public int StateTypeSize;
            public PagedStore Store;
            public Action<IntPtr, IntPtr[]> Pipeline;
        }
    }
}
