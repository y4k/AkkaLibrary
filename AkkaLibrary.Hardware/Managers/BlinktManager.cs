using Akka.Actor;
using AkkaLibrary.Hardware.Exceptions;
using AkkaLibrary.Hardware.StaticWrappers;

namespace AkkaLibrary.Hardware.Managers
{
    /// <summary>
    /// Manager for a BlinktPhat
    /// </summary>
    public class BlinktManager : ReceiveActor
    {
        private readonly BlinktPhat _manager;

        public BlinktManager()
        {
            if(BlinktPhat.Instance.Initialise())
            {
                throw new HardwareInitialisationException();
            }
            _manager = BlinktPhat.Instance;

            Receive<On>(msg => _manager.OnAll(msg.Red, msg.Green, msg.Blue));

            Receive<Off>(msg => _manager.OffAll());

            Receive<OnPixel>(msg => _manager.OnPixel(msg.Pixel, msg.Red, msg.Green, msg.Blue));

            Receive<OffPixel>(msg => _manager.OffPixel(msg.Pixel));

            Receive<SetPixel>(msg => _manager.SetPixel(msg.Pixel, msg.Red, msg.Green, msg.Blue));

            Receive<SetPixels>(msg => _manager.SetPixels(msg.Red, msg.Green, msg.Blue));

            Receive<Update>(msg => _manager.Update());
        }

        public override void AroundPostStop()
        {
            BlinktPhat.Instance.Shutdown();
        }

        #region Messages

        /// <summary>
        /// Base class containg Red, Green and Blue pixel values
        /// </summary>
        public abstract class BlinktRGB
        {
            public short Red { get; }
            public short Green { get; }
            public short Blue { get; }

            protected BlinktRGB(short red, short green, short blue)
            {
                Red = red;
                Green = green;
                Blue = blue;
            }
        }

        /// <summary>
        /// Activates all the pixels with a given color
        /// </summary>
        public sealed class On : BlinktRGB
        {
            public On(short red, short green, short blue)
             : base(red, green, blue) { }
        }

        /// <summary>
        /// Deactivates all pixels
        /// </summary>
        public sealed class Off { }

        /// <summary>
        /// Activates a single pixel with the given color
        /// </summary>
        public sealed class OnPixel : BlinktRGB, IPixelSelector
        {
            public short Pixel { get; }

            public OnPixel(short pixel, short red, short green, short blue)
             : base(red, green, blue)
            {
                Pixel = pixel;
            }
        }

        /// <summary>
        /// Deactivates a given pixel
        /// </summary>
        public sealed class OffPixel : IPixelSelector
        {
            public short Pixel { get; }

            public OffPixel(short pixel)
            {
                Pixel = pixel;
            }
        }

        /// <summary>
        /// Sets all pixels to a given color without updating
        /// </summary>
        public sealed class SetPixels : BlinktRGB
        {
            public SetPixels(short red, short green, short blue) : base(red, green, blue)
            {
            }
        }

        /// <summary>
        /// Sets a single pixel to a given color without updating
        /// </summary>
        public sealed class SetPixel : BlinktRGB, IPixelSelector
        {
            public short Pixel { get; }

            public SetPixel(short pixel, short red, short green, short blue)
             : base(red, green, blue)
            {
                Pixel = pixel;
            }
        }

        /// <summary>
        /// Updates all pixels with any changes that have been made via
        /// SetPixel(s)
        /// </summary>
        public sealed class Update { }

        /// <summary>
        /// Adds a Pixel property
        /// </summary>
        interface IPixelSelector
        {
            short Pixel { get; }
        }

        #endregion
    }
}