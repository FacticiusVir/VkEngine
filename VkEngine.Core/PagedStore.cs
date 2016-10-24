using System;
using System.Runtime.InteropServices;

namespace VkEngine
{
    public class PagedStore
        : IDisposable
    {
        private readonly Type dataType;
        private readonly uint dataSize;
        private readonly Page[] pages;

        public PagedStore(int pageCount, Type dataType)
        {
            this.dataType = dataType;
            this.dataSize = MemUtil.SizeOf(dataType);
            this.pages = new Page[pageCount];
        }

        public void UpdateCapacity(PageWriteKey key, int requiredCapacity)
        {
            Page writePage = this.pages[key.WritePage];

            if (writePage.Capacity < requiredCapacity)
            {
                int scaledCapacity = FindScaledCapacity(requiredCapacity);

                long scaledSize = scaledCapacity * this.dataSize;

                Page newPage = new Page
                {
                    Capacity = scaledCapacity
                };

                if (writePage.Data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(newPage.Data);
                }

                newPage.Data = Marshal.AllocHGlobal((IntPtr)scaledSize);

                this.pages[key.WritePage] = newPage;
            }
        }

        public IntPtr GetReadPage(PageWriteKey key)
        {
            return this.pages[key.ReadPage].Data;
        }

        public IntPtr GetWritePage(PageWriteKey key)
        {
            return this.pages[key.WritePage].Data;
        }

        private int FindScaledCapacity(int minimumCapacity)
        {
            return 1 << (int)Math.Ceiling(Math.Log(minimumCapacity, 2));
        }

        public void Dispose()
        {
            for (int index = 0; index < this.pages.Length; index++)
            {
                if (this.pages[index].Data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(this.pages[index].Data);
                }
            }
        }

        private struct Page
        {
            public IntPtr Data;
            public int Capacity;
        }
    }
}
