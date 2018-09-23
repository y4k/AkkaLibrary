using System.Runtime.InteropServices;

namespace AkkaLibrary.Hardware.StaticWrappers
{
    /// <summary>
    /// Wrapper class around external C++ functions to control Inky Phat
    /// (Pimoroni) on a Raspberry Pi
    /// 
    /// Requires the accompanying inkyphat cpp libraries and header and
    /// the WiringPi library instead on the Raspberry Pi
    /// 
    /// Uses SPI which must also be enabled on the Pi
    /// </summary>
    internal static class InkyPhatWrapper
    {
        /// <summary>
        /// Initialisation function for InkyPhat
        /// </summary>
        /// <returns>0 for success, otherwise a fault code</returns>
        [DllImport("SharedLibraries/build/libinkyphat.so", EntryPoint = "init")]
        public static extern int Initialise();

        /// <summary>
        /// Draw function for InkyPhat
        /// </summary>
        /// <returns>0 for success, otherwise a fault code</returns>
        [DllImport("SharedLibraries/build/libinkyphat.so", EntryPoint = "draw")]
        public static extern int Draw(byte[] bytes);

        /// <summary>
        /// Shutdown function for InkyPhat
        /// </summary>
        /// <returns>0 for success, otherwise a fault code</returns>
        [DllImport("SharedLibraries/build/libinkyphat.so", EntryPoint = "shutdown")]
        public static extern int Shutdown();
    }
}