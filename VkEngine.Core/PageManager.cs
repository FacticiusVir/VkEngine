using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkEngine
{
    public class PageManager
    {
        private readonly object lockObject = new object();
        private LockRecord[] pageLocks;

        public PageManager(int pageCount)
        {
            this.pageLocks = new LockRecord[pageCount];
        }

        public PageReadKey GetReadKey()
        {
            return new PageReadKey(this.GetReadPage());
        }

        public PageWriteKey GetWriteKey()
        {
            int readPage = this.GetReadPage();
            int writePage = this.GetWritePage();

            return new PageWriteKey(readPage, writePage);
        }

        public void Release(PageReadKey readKey)
        {
            this.ReleaseLock(readKey.ReadPage);
        }

        public void Release(PageWriteKey writeKey)
        {
            this.ReleaseLock(writeKey.WritePage);
            this.ReleaseLock(writeKey.ReadPage);
        }

        private void ReleaseLock(int pageIndex)
        {
            lock (this.lockObject)
            {
                LockRecord record = this.pageLocks[pageIndex];

                record.LockCount--;

                if (record.LockCount == 0)
                {
                    record.Lock = LockType.None;
                }

                this.pageLocks[pageIndex] = record;
            }
        }

        private int GetReadPage()
        {
            long mostRecentSequence = -1;
            int readPageIndex = -1;

            lock (this.lockObject)
            {
                for (int pageIndex = 0; pageIndex < this.pageLocks.Length; pageIndex++)
                {
                    LockRecord record = this.pageLocks[pageIndex];

                    if (record.Lock != LockType.Write
                            && record.Sequence > mostRecentSequence)
                    {
                        mostRecentSequence = record.Sequence;
                        readPageIndex = pageIndex;
                    }
                }

                if (readPageIndex == -1)
                {
                    throw new Exception("No page available for reading.");
                }
                else
                {
                    LockRecord record = this.pageLocks[readPageIndex];

                    this.pageLocks[readPageIndex] = new LockRecord
                    {
                        Lock = LockType.Read,
                        LockCount = record.LockCount + 1,
                        Sequence = record.Sequence
                    };

                    return readPageIndex;
                }
            }
        }

        private int GetWritePage()
        {
            long maxSequence = -1;
            long leastRecentSequence = long.MaxValue;
            int writePageIndex = -1;

            lock (this.lockObject)
            {
                for (int pageIndex = 0; pageIndex < this.pageLocks.Length; pageIndex++)
                {
                    LockRecord record = this.pageLocks[pageIndex];

                    if (record.Lock == LockType.None
                            && record.Sequence < leastRecentSequence)
                    {
                        leastRecentSequence = record.Sequence;
                        writePageIndex = pageIndex;
                    }

                    if (record.Sequence > maxSequence)
                    {
                        maxSequence = record.Sequence;
                    }
                }

                if (writePageIndex == -1)
                {
                    throw new Exception("No page available for writing.");
                }
                else
                {
                    this.pageLocks[writePageIndex] = new LockRecord
                    {
                        Lock = LockType.Write,
                        LockCount = 1,
                        Sequence = maxSequence + 1
                    };

                    return writePageIndex;
                }
            }
        }

        private struct LockRecord
        {
            public LockType Lock;
            public int LockCount;
            public long Sequence;
        }

        private enum LockType
        {
            None,
            Read,
            Write
        }
    }
}
