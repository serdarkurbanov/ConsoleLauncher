using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleLauncher.Processes
{
    // record of using CPU, Memory (private set), Disk, thread count
    public class ResourceUsageRecord
    {
        public ResourceUsageRecord(double data)
        {
            Data = data;
            Time = DateTime.Now;
            ID = System.Threading.Interlocked.Increment(ref _id);
        }

        private static long _id = 0;
        // thread safe counter
        public long ID { get; private set; }

        public double Data { get; set; }
        public DateTime Time { get; set; }
    }
}
