using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkEngine
{
    public class PageManager
    {
        private LockType[] pageLocks;

        public PageManager(int pageCount)
        {

        }

        private enum LockType
        {
            None,
            Read,
            Write
        }
    }
}
