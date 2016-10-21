namespace VkEngine
{
    public struct PageReadKey
    {
        public readonly int ReadPage;

        public PageReadKey(int readPage, int writePage)
        {
            this.ReadPage = readPage;
        }
    }
}
