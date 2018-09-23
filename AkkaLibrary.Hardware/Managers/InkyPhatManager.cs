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
        private readonly IInkyPhatController _inkyController;
        private readonly ILoggingAdapter _logger;
        private long _currentDrawId = 0;

        /// <inheritdoc/>
        public InkyPhatManager(IInkyPhatController inkyController = null)
        {
            _logger = Context.WithIdentity("InkyPhatManager");
            _inkyController = inkyController ?? InkyPhat.Instance;
            if (!_inkyController.Initialise())
            {
                throw new HardwareInitialisationException();
            }
            
            Become(AcceptingDraw);
        }

        /// <inheritdoc/>
        private void DrawingInProgress()
        {
            Receive<Draw>(msg =>
            {
                _logger.Debug("Received a new draw request while currently executing draw:{DrawId}", _currentDrawId);
                Sender.Tell(new DrawRejected(_currentDrawId));
            });

            Receive<DrawComplete>(msg => 
            {
                msg.DrawRequester.Tell(msg);
                if(msg.Success)
                {
                    _logger.Debug("Successfully completed draw:{DrawId}", msg.DrawId);
                }
                else
                {
                    _logger.Warning("Did not successfully complete draw:{DrawId}", msg.DrawId);
                }
                _currentDrawId++;
                Become(AcceptingDraw);
            });
        }

        /// <inheritdoc/>
        private void AcceptingDraw()
        {
            Receive<Draw>(msg =>
            {
                var sender = Sender;
                var id = _currentDrawId;
                Task.Run(() => (id, _inkyController.Draw(msg.Pixels)))
                .ContinueWith(task => new DrawComplete(task.Result, sender))
                .PipeTo(Self, Self);
                Sender.Tell(new DrawAccepted(id));
                Become(DrawingInProgress);
            });
        }

        /// <summary>
        /// Once the actor is stopped, the InkyInstance is shutdown gracefully
        /// </summary>
        public override void AroundPostStop()
        {
            _inkyController.Shutdown();
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
        public sealed class DrawRejected
        {
            public long DrawId { get; }

            public DrawRejected(long currentDrawId)
            {
                DrawId = currentDrawId;
            }
        }

        /// <summary>
        /// Class that indicates the success or failure of
        /// a draw call to InkyPhat
        /// </summary>
        public sealed class DrawComplete
        {
            public long DrawId { get; }
            public bool Success { get; }
            public IActorRef DrawRequester { get; }

            public DrawComplete((long id, bool success) result, IActorRef requester)
             : this(result.id, result.success, requester) { }

            public DrawComplete(long id, bool success, IActorRef requester)
            {
                DrawId = id;
                Success = success;
                DrawRequester = requester;
            }
        }

        /// <summary>
        /// Sent to the requester of a draw to indicate that
        /// the draw update has been accepted
        /// </summary>
        public sealed class DrawAccepted
        {
            public long DrawId;

            public DrawAccepted(long drawId)
            {
                DrawId = drawId;
            }
        }

        #endregion
    }
}