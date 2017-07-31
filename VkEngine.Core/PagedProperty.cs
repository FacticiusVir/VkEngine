namespace VkEngine
{
    public struct PagedProperty<T>
    {
        private readonly T[] values;

        public PagedProperty(int pageCount)
        {
            this.values = new T[pageCount];
        }

        public T Get(PageReadKey key)
        {
            return this.values[key.ReadPage];
        }

        public T Get(PageWriteKey key)
        {
            return this.values[key.ReadPage];
        }

        public T GetNew(PageWriteKey key)
        {
            return this.values[key.WritePage];
        }

        public void Set(PageWriteKey key, T value)
        {
            this.values[key.WritePage] = value;
        }

        public T Copy(PageWriteKey key)
        {
            return this.values[key.WritePage] = this.values[key.ReadPage];
        }
    }
}
