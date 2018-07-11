using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Hardware.Exceptions;
using AkkaLibrary.Hardware.StaticWrappers;

namespace AkkaLibrary.Hardware.Managers
{
    /// <summary>
    /// Actor manager for InkyPhat hardware
    /// 
    /// State-machine style that initialises on instantiation
    /// Once an update is requested, further updates will be ignored until
    /// the original has finished processing
    /// </summary>
    public class InkyPhatManager : ReceiveActor
    {
        private readonly InkyPhat _manager;
        private readonly ILoggingAdapter _logger;
        private long _currentDrawId = 0;

        public InkyPhatManager()
        {
            _logger = Context.WithIdentity("InkyPhatManager");
            if (InkyPhat.Instance.Initialise())
            {
                throw new HardwareInitialisationException();
            }
            _manager = InkyPhat.Instance;

            Become(AcceptingDraw);
        }

        private void DrawingInProgress()
        {
            Receive<Draw>(msg =>
            {
                _logger.Debug("Received a new draw request while currently executing draw:{DrawId}", _currentDrawId);
                Sender.Tell(new CurrentlyDrawing(_currentDrawId));
            });

            Receive<DrawComplete>(msg => 
            {
                if(msg.Success)
                {
                    _logger.Debug("Successfully completed draw:{DrawId}", msg.Id);
                }
                else
                {
                    _logger.Warning("Did not successfully complete draw:{DrawId}", msg.Id);
                }
                Become(AcceptingDraw);
            });
        }

        private void AcceptingDraw()
        {
            Receive<Draw>(msg =>
            {
                Task.Run(() => (_currentDrawId++, _manager.Draw(msg.Pixels)))
                .ContinueWith(task => new DrawComplete(task.Result))
                .PipeTo(Self, Self);
                Become(DrawingInProgress);
            });
        }

        public override void AroundPostStop()
        {
            InkyPhat.Instance.Shutdown();
        }

        #region Messages

        /// <summary>
        /// Updates InkyPhat display with stored pixel values
        /// </summary>
        public sealed class Draw
        {
            public InkyPhatColours[,] Pixels { get; }
            
            public Draw(InkyPhatColours[,] pixels)
            {
                Pixels = pixels;
            }
        }

        /// <summary>
        /// Message sent back to sender of Draw call
        /// that indicate InkyPhat is currently drawing
        /// </summary>
        public sealed class CurrentlyDrawing
        {
            public long CurrentDrawId { get; }

            public CurrentlyDrawing(long currentDrawId)
            {
                CurrentDrawId = currentDrawId;
            }
        }

        /// <summary>
        /// Class that indicates the success or failure of
        /// a draw call to InkyPhat
        /// </summary>
        public sealed class DrawComplete
        {
            public long Id { get; }
            public bool Success { get; }

            public DrawComplete((long id, bool success) result)
             : this(result.id, result.success) { }

            public DrawComplete(long id, bool success)
            {
                Id = id;
                Success = success;
            }
        }

        #endregion
    }
}