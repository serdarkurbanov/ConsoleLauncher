using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleLauncher.Processes
{
    // viewmodel for one line received from the process
    public class OutputRecord
    {
        private static long _id = 0;

        public DateTime Time { get; set; }

        public DateTime TimeProcessStart { get; set; }

        public TimeSpan TotalProcessorTime { get; set; }

        public long ProcessVirtualMemory { get; set; }

        public int ProcessThreadCount { get; set; }

        public string Content { get; set; }

        // thread safe counter
        public long ID { get; private set; }

        public RecordType RecordType { get; set; }

        public static OutputRecord FromDataReceived(System.Diagnostics.DataReceivedEventArgs data, RecordType recType)
        {
            return new OutputRecord()
            {
                Time = DateTime.Now,
                Content = data.Data,
                ID = System.Threading.Interlocked.Increment(ref _id),
                RecordType = recType
            };
        }

    }

    public enum RecordType
    {
        Info,
        Error
    }


}
