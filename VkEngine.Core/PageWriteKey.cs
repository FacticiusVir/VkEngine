namespace VkEngine
{
    public struct PageWriteKey
    {
        public readonly int ReadPage;
        public readonly int WritePage;

        public PageWriteKey(int readPage, int writePage)
        {
            this.ReadPage = readPage;
            this.WritePage = writePage;
        }
    }
}
