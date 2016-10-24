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

        public IntPtr GetReadPage(PageWriteKey key)
        {
            return this.pages[key.ReadPage].Data;
        }

        public IntPtr GetWritePage(PageWriteKey key)
        {
            return this.pages[key.WritePage].Data;
        }

        public int GetWriteCapacity(PageWriteKey key)
        {
            return this.pages[key.WritePage].Capacity;
        }

        public void UpdateWriteCapacity(PageWriteKey key, int requiredCapacity, bool persistPage = false)
        {
            Page writePage = this.pages[key.WritePage];

            if (writePage.Capacity < requiredCapacity)
            {
                long requiredSize = requiredCapacity * this.dataSize;

                Page newPage = new Page
                {
                    Capacity = requiredCapacity
                };

                if (!persistPage && writePage.Data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(newPage.Data);
                    writePage.Data = IntPtr.Zero;
                }

                if (writePage.Data == IntPtr.Zero)
                {
                    newPage.Data = Marshal.AllocHGlobal((IntPtr)requiredSize);
                }
                else
                {
                    newPage.Data = Marshal.ReAllocHGlobal(writePage.Data, (IntPtr)requiredSize);
                }

                this.pages[key.WritePage] = newPage;
            }
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
