using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VkEngine
{
    public static class MemUtil
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static uint SizeOf<T>()
        {
            return SizeOfCache<T>.SizeOf;
        }
        
        private static class SizeOfCache<T>
        {
            public static readonly uint SizeOf;

            static SizeOfCache()
            {
                uint value = SizeOf(typeof(T));
                SizeOf = value;
            }
        }

        public static uint SizeOf(Type type)
        {
            var dm = new DynamicMethod("func", typeof(int),
                                                       Type.EmptyTypes, typeof(MemUtil));

            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ret);

            var func = (Func<int>)dm.CreateDelegate(typeof(Func<int>));

            return (uint)func();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void ReadFromPtr<T>(IntPtr source, out T value)
            where T : struct
        {
            value = default(T);

            uint size = SizeOf<T>();

            void* pointer = source.ToPointer();

            TypedReference valueReference = __makeref(value);
            void* valuePointer = (*((IntPtr*)&valueReference)).ToPointer();

            System.Buffer.MemoryCopy(pointer, valuePointer, size, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void WriteToPtr<T>(IntPtr dest, T value)
            where T : struct
        {
            uint size = SizeOf<T>();

            void* pointer = dest.ToPointer();

            TypedReference valueReference = __makeref(value);
            void* valuePointer = (*((IntPtr*)&valueReference)).ToPointer();

            System.Buffer.MemoryCopy(valuePointer, pointer, size, size);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void WriteToPtr<T>(IntPtr dest, T[] value, int startIndex, int count)
            where T : struct
        {
            if (count > 0)
            {
                int elementSize = (int)SizeOf<T>();
                int transferSize = elementSize * value.Length;

                void* pointer = dest.ToPointer();

                var handle = GCHandle.Alloc(value, GCHandleType.Pinned);

                byte* handlePointer = (byte*)handle.AddrOfPinnedObject().ToPointer();

                System.Buffer.MemoryCopy(handlePointer + (elementSize * startIndex), pointer, transferSize, transferSize);

                handle.Free();
            }
        }
    }
}
