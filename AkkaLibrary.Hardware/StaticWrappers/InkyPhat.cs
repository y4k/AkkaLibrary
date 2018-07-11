using System;
using System.Collections.Generic;
using System.Linq;
using AkkaLibrary.Common.Logging;
using Serilog;

namespace AkkaLibrary.Hardware.StaticWrappers
{
    /// <summary>
    /// Singleton class that controls InkyPhat hardware on a Raspberry Pi
    /// </summary>
    public class InkyPhat
    {
        // The singleton instance
        private static InkyPhat _instance;

        /// <summary>
        /// Public accessor to the singleton instance
        /// If that instance does not exist, instantiates it
        /// </summary>
        /// <returns>InkyPhat</returns>
        public static InkyPhat Instance
            => _instance == null ? _instance = new InkyPhat() : _instance;

        private volatile bool _running = false;
        private object _lock = new object();
        private ILogger _logger;

        protected InkyPhat()
            => _logger = LoggerFactory.Logger.WithIdentity("InkyPhat");

        /// <summary>
        /// Initialises the InkyPhat library
        /// </summary>
        /// <returns>true if initialised correctly, false otherwise</returns>
        public bool Initialise()
        {
            lock(_lock)
            {
                if (_running)
                {
                    if (InkyPhatWrapper.Initialise() == 0 ? _running = true: _running = false)
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
        /// Shuts down the InkyPhat library
        /// </summary>
        /// <returns>true if shutdown correctly, false otherwise</returns>
        public bool Shutdown()
        {
            lock(_lock)
            {
                if (_running)
                {
                    if(InkyPhatWrapper.Shutdown() == 0 ? _running = true: _running = false)
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
        /// Updates the InkyPhat display with the pixel array
        /// </summary>
        /// <returns>true if called correctly, false otherwise</returns>
        public bool Draw(InkyPhatColours[,] pixels)
        {
            lock(_lock)
            {
                if (_running)
                {
                    if(pixels.GetLength(0) != Width || pixels.GetLength(1) != Height)
                    {
                        return false;
                    }

                    if(InkyPhatWrapper.Draw(pixels.Enumerate().Select(x => (byte)x).ToArray()) != 0)
                    {
                        _logger.Debug("Draw called");
                        return true;
                    }
                    else
                    {
                        _logger.Error("Draw exited incorrectly");
                        return false;
                    }
                }
                _logger.Warning("Draw called while not running");
                return false;
            }
        }

        private static readonly int Width = 104;
        private static readonly int Height = 212;
    }

    internal static class EnumeratorExtensions
    {
        public static IEnumerable<T> Enumerate<T>(this T[,] array)
        {
            var enumerator = array.GetEnumerator();
            while(enumerator.MoveNext())
            {
                yield return (T)enumerator.Current;
            }
        }
    }

    public enum InkyPhatColours
    {
        WHITE = 0,
        RED = 1,
        BLACK = 2
    }
}