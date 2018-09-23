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
    public class InkyPhat : IInkyPhatController
    {
        public static readonly int Width = 104;
        public static readonly int Height = 212;
        
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
    }

    /// <summary>
    /// Controller interface for InkyPhat
    /// </summary>
    public interface IInkyPhatController
    {
        /// <summary>
        /// Initialises InkyPhat controller
        /// Further functions should not be callable unless
        /// initialised
        /// </summary>
        /// <returns>True if initialised</returns>
        bool Initialise();
        
        /// <summary>
        /// Takes a 
        /// </summary>
        /// <param name="pixels"></param>
        /// <returns>True if draw successful</returns>
        bool Draw(InkyPhatColours[,] pixels);
        
        /// <summary>
        /// Uninitialises and frees resources associated with
        /// InkyPhat
        /// 
        /// Requires Initialise to be called again before InkyPhat
        /// can be used again
        /// </summary>
        /// <returns>True if shutdown successfully</returns>
        bool Shutdown();
    }

    /// <summary>
    /// Extensions to aid dealing with multi
    /// </summary>
    public static class MultiDimensionalArrayExtensions
    {
        /// <summary>
        /// Takes a multidimensional array and flattens it by iterating over
        /// each of the inner arrays in order
        /// </summary>
        /// <param name="T[,]">Rank 2 array</param>
        /// <returns name="IEnumerable<T>">Flattened array as IEnumerable</returns>
        public static IEnumerable<T> Enumerate<T>(this T[,] array)
        {
            var enumerator = array.GetEnumerator();
            while(enumerator.MoveNext())
            {
                yield return (T)enumerator.Current;
            }
        }
    }

    /// <summary>
    /// Defines colours used on InkyPhat
    /// </summary>
    public enum InkyPhatColours
    {
        WHITE = 0,
        RED = 1,
        BLACK = 2
    }
}