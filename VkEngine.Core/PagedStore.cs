using System;

namespace VkEngine
{
    public class PagedStore<T>
        where T : struct
    {
        private T[][] pages;

        public PagedStore(int pageCount, int initialCapacity)
        {
            this.pages = new T[pageCount][];

            int scaledCapacity = FindScaledCapacity(initialCapacity);

            for (int index = 0; index < pageCount; index++)
            {
                this.pages[index] = new T[initialCapacity];
            }
        }

        public void UpdateCapacity(PageWriteKey key, int requiredCapacity)
        {
            if (this.pages[key.WritePage].Length < requiredCapacity)
            {
                int scaledCapacity = FindScaledCapacity(requiredCapacity);

                this.pages[key.WritePage] = new T[scaledCapacity];
            }
        }

        public T[] GetReadPage(PageWriteKey key)
        {
            return this.pages[key.ReadPage];
        }

        public T[] GetWritePage(PageWriteKey key)
        {
            return this.pages[key.WritePage];
        }

        private int FindScaledCapacity(int minimumCapacity)
        {
            return 1 << (int)Math.Ceiling(Math.Log(minimumCapacity, 2));
        }
    }
}
