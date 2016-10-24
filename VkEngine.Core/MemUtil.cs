using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VkEngine
{
    /// <summary>
    /// 
    /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dest"></param>
        /// <param name="value"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dest"></param>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
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
