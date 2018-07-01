using AkkaLibrary.Common.Logging;
using Serilog;

namespace AkkaLibrary.Hardware.StaticWrappers
{
    /// <summary>
    /// Singleton around the BlinktPhat wrapper that provides logging and
    /// thread-safety
    /// </summary>
    public class BlinktPhat
    {
        // The singleton instance
        private static BlinktPhat _instance;

        /// <summary>
        /// Public accessor to the singleton instance
        /// If that instance does not exist, instantiates it
        /// </summary>
        /// <returns>BlinktPhat</returns>
        public static BlinktPhat Instance
            => _instance == null ? _instance = new BlinktPhat() : _instance;
        
        private volatile bool running = false;
        private object _lock = new object();
        private ILogger _logger;

        protected BlinktPhat()
        {
            _logger = LoggerFactory.Logger.WithIdentity("BlinktPhat");
        }

        /// <summary>
        /// Initialises the Blinkt Phat library
        /// </summary>
        /// <returns>true if initialised correctly, false otherwise</returns>
        public bool Initialise()
        {
            lock(_lock)
            {
                if (running)
                {
                    if (BlinktPhatWrapper.Initialise() == 0 ? running = true: running = false)
                    {
                        _logger.Debug("Initialised correctly");
                        return true;
                    }
                    else
                    {
                        _logger.Error("Could not be initialised correctly");
                        return false;
                    }
                }
                _logger.Warning("Initialise called while already running");
                return false;
            }
        }

        /// <summary>
        /// Shuts down the Blinkt Phat library
        /// </summary>
        /// <returns>true if shutdown correctly, false otherwise</returns>
        public bool Shutdown()
        {
            lock(_lock)
            {
                if (running)
                {
                    if(BlinktPhatWrapper.Shutdown() == 0 ? running = true: running = false)
                    {
                        _logger.Debug("Shutdown correctly");
                        return true;
                    }
                    else
                    {
                        _logger.Error("Could not shutdown correctly");
                        return false;
                    }
                }
                _logger.Warning("Shutdown called while not running");
                return false;
            }
        }

        /// <summary>
        /// Turns on all pixels with the given (r,g,b) combination
        /// </summary>
        /// <returns>true if called correctly, false otherwise</returns>
        public bool OnAll(short r, short g, short b)
        {
            lock(_lock)
            {
                if (running)
                {
                    if(BlinktPhatWrapper.OnAll(r, g, b, 3) == 0)
                    {
                        _logger.Debug("All pixels set:({Red},{Green},{Blue})", r, g, b);
                        return true;
                    }
                    else
                    {
                        _logger.Error("OnAll exited incorrectly");
                        return false;
                    }
                }
                _logger.Warning("OnAll called while not running");
                return false;
            }
        }

        /// <summary>
        /// Turns on a single pixel with the given (r,g,b) combination
        /// </summary>
        /// <returns>true if called correctly, false otherwise</returns>
        public bool OnPixel(short pixel, short r, short g, short b)
        {
            lock(_lock)
            {
                if (running)
                {
                    if(BlinktPhatWrapper.OnPixel(pixel, r, g, b, 3) != 0)
                    {
                        _logger.Debug("OnPixel {Pixel} set:({Red},{Green},{Blue})", pixel, r, g, b);
                        return true;
                    }
                    else
                    {
                        _logger.Error("OnPixel {Pixel} set:({Red},{Green},{Blue}) exited incorrectly", pixel, r, g, b);
                        return false;
                    }
                }
                _logger.Warning("OnPixel called while not running");
                return false;
            }
        }

        /// <summary>
        /// Turns off all pixels
        /// </summary>
        /// <returns>true if called correctly, false otherwise</returns>
        public bool OffAll()
        {
            lock(_lock)
            {
                if (running)
                {
                    if(BlinktPhatWrapper.OffAll() != 0)
                    {
                        _logger.Debug("OffAll exited correctly");
                        return true;
                    }
                    else
                    {
                        _logger.Error("OffAll exited incorrectly");
                        return false;
                    }
                }
                _logger.Warning("OffAll called while not running");
                return false;
            }
        }

        /// <summary>
        /// Turns off a single pixel
        /// </summary>
        /// <returns>true if called correctly, false otherwise</returns>
        public bool OffPixel(short pixel)
        {
            lock(_lock)
            {
                if (running)
                {
                    if(BlinktPhatWrapper.OffPixel(pixel) != 0)
                    {
                        _logger.Debug("OffPixel:{Pixel}", pixel);
                        return true;
                    }
                    else
                    {
                        _logger.Warning("OffPixel:{pixel} exited incorrectly", pixel);
                        return false;
                    }
                }
                _logger.Warning("OffPixel called while not running");
                return false;
            }
        }

        /// <summary>
        /// Sets the (r,g,b) value for all pixels without updating
        /// </summary>
        /// <returns>true if called correctly, false otherwise</returns>
        public bool SetPixels(short r, short g, short b)
        {
            lock(_lock)
            {
                if (running)
                {
                    if(BlinktPhatWrapper.SetPixels(r, g, b, 3) != 0)
                    {
                        _logger.Debug("SetPixels set:({Red},{Green},{Blue})", r, g, b);
                        return true;
                    }
                    else
                    {
                        _logger.Error("SetPixels set:({Red},{Green},{Blue}) exited incorrectly", r, g, b);
                        return false;
                    }
                }
                _logger.Warning("SetPixels called while not running");
                return false;
            }
        }

        /// <summary>
        /// Sets the (r,g,b) value for a given pixel without updating
        /// </summary>
        /// <returns>true if called correctly, false otherwise</returns>
        public bool SetPixel(short pixel, short r, short g, short b)
        {
            lock(_lock)
            {
                if (running)
                {
                    if(BlinktPhatWrapper.SetPixel(pixel, r, g, b, 3) != 0)
                    {
                        _logger.Debug("SetPixel {Pixel} set:({Red},{Green},{Blue})", pixel, r, g, b);
                        return true;
                    }
                    else
                    {
                        _logger.Error("SetPixel {Pixel} set:({Red},{Green},{Blue}) exited incorrectly", pixel, r, g, b);
                        return false;
                    }
                }
                _logger.Warning("SetPixel called while not running");
                return false;
            }
        }

        /// <summary>
        /// Updates all pixels with any (r,g,b) value changes that have been applied
        /// </summary>
        /// <returns>true if called correctly, false otherwise</returns>
        public bool Update()
        {
            lock(_lock)
            {
                if (running)
                {
                    if(BlinktPhatWrapper.Update() != 0)
                    {
                        _logger.Debug("Update called");
                        return true;
                    }
                    else
                    {
                        _logger.Error("Update exited incorrectly");
                        return false;
                    }
                }
                _logger.Warning("Update called while not running");
                return false;
            }
        }
    }
}