using System;

namespace DigitAppCore
{
    public class LogEntry
    {
        public DateTime OccurenceTime { get; set; }
        public DateTime LogTime { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
    }
}
