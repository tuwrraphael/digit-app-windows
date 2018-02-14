using System;

namespace DigitAppCore
{
    [Serializable]
    public class DigitBLEExpcetion : Exception
    {
        public DigitBLEExpcetion()
        {
        }

        public DigitBLEExpcetion(string message) : base(message)
        {
        }

        public DigitBLEExpcetion(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}