using System.Runtime.InteropServices;

namespace AkkaLibrary.Hardware.StaticWrappers
{
    /// <summary>
    /// Wrapper class around external C++ functions to control Blinkt Phat
    /// (Pimoroni) on a Raspberry Pi
    /// 
    /// Requires the accompanying blink cpp libraries and headers and the bcm2835
    /// library installed on the Raspbery Pi
    /// </summary>
    internal static class BlinktPhatWrapper
    {
        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "init")]
        public static extern int Initialise();

        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "shutdown")]
        public static extern int Shutdown();

        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "on_all")]
        public static extern int OnAll(short r, short g, short b, short br);

        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "on_pixel")]
        public static extern int OnPixel(short pixel, short r, short g, short b, short br);

        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "off_all")]
        public static extern int OffAll();

        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "off_pixel")]
        public static extern int OffPixel(short pixel);

        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "set_pixels")]
        public static extern int SetPixels(short r, short g, short b, short br);

        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "set_pixel")]
        public static extern int SetPixel(short pixel, short r, short g, short b, short br);

        [DllImport("SharedLibraries/build/libblinkt.so", EntryPoint = "update")]
        public static extern int Update();
    }
}