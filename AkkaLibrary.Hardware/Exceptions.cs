using System;

namespace AkkaLibrary.Hardware.Exceptions
{
    public sealed class HardwareInitialisationException : Exception
    {
        public HardwareInitialisationException() : base() { }

        public HardwareInitialisationException(string message) : base(message)
        {
            
        }
    }
}