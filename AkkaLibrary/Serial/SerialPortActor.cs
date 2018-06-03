using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using RJCP.IO.Ports;
using NETCoreAsio.DataStructures;
using Akka.Event;
using AkkaLibrary.Common.Logging;

namespace AkkaLibrary.Serial
{
    public class SerialPortReadActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;

        public SerialPortReadActor(string name, IActorRef supervisor)
        {
            _buffer = new CircularQueue<byte>(10000, false);
            _supervisor = supervisor;

            _logger = Context.WithIdentity("SerialPortReader");

            Ready();
        }

        private void Ready()
        {
            Receive<OpenPortRequest>(msg =>
            {
                var port = new SerialPortStream();
                port.PortName = msg.PortName;
                port.BaudRate = msg.BaudRate;
                port.Encoding = Encoding.ASCII;

                try
                {
                    port.Open();
                    if(port.IsOpen)
                    {
                        _port = port;
                        Become(Running);
                        Self.Tell(new AsyncReceive());
                    }
                }
                catch(Exception ex)
                {
                    _logger.Warning($"Failed to open serial port {port.PortName}.", ex);
                    Sender.Tell(new PortOpenFailure(msg.PortName, msg.BaudRate, ex.Message));
                }
            });
        }

        private void Running()
        {
            Receive<AsyncReceive>(msg =>
            {
                //Read into buffer
                var buffer = _buffer.ArraySegmentsFree.First();
                
                _port
                .ReadAsync(buffer.Array, buffer.Offset, buffer.Count)
                .ContinueWith(bytes =>
                {
                    var bytesTransferred = bytes.Result;
                    _buffer.UpdateAfterItemsReceived(bytesTransferred);

                    var resultBuffer = new byte[bytesTransferred];

                    _buffer.CopyTo(0, resultBuffer, 0, bytesTransferred);
                    _buffer.Drop(bytesTransferred);
                    
                    _logger.Info($"Buffer Count:{_buffer.Count} and number of bytes parsed:{bytesTransferred}");

                    Self.Tell(new AsyncReceive());

                    return new PortReadResult(_port.PortName,resultBuffer);
                }, TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously)
                .PipeTo(_supervisor);
            });

            Receive<ClosePortRequest>(result =>
            {
                ClosePort();
                Become(Ready);
            });

            Receive<OpenPortRequest>(msg =>
            {
                _logger.Error($"Port {_port.PortName} is already open. Request that this port be closed and reopened or open another port.");
            });
        }

        private void ClosePort()
        {
            _logger.Info($"Closing serial port {_port.PortName}");            
            _port.Close();
            _port.Dispose();
        }

        public override void AroundPostStop()
        {
            ClosePort();            
            base.AroundPostStop();
        }

        private SerialPortStream _port;
        private CircularQueue<byte> _buffer;
        private readonly IActorRef _supervisor;


        public static Props GetProps(string name, IActorRef supervisor) => Props.Create(() => new SerialPortReadActor(name, supervisor));


        #region Messages

        public sealed class OpenPortRequest
        {
            public string PortName { get; }

            public int BaudRate { get; }

            public OpenPortRequest(string name, int baudRate)
            {
                PortName = name;
                BaudRate = baudRate;
            }
        }

        public sealed class ClosePortRequest { }

        public sealed class PortOpenSuccess
        {
            public string PortName { get; }

            public int BaudRate { get; }

            public PortOpenSuccess(string name, int baudRate)
            {
                PortName = name;
                BaudRate = baudRate;
            }
        }

        public sealed class PortOpenFailure
        {
            public string PortName { get; }

            public int BaudRate { get; }

            public string Reason { get; }

            public PortOpenFailure(string name, int baudRate, string reason)
            {
                PortName = name;
                BaudRate = baudRate;
                Reason = reason;
            }
        }

        private sealed class AsyncReceive { }

        public sealed class PortReadResult
        {
            public string PortName { get; }
            public byte[] Buffer { get; }

            public PortReadResult(string portName, byte[] buffer)
            {
                PortName = portName;
                Buffer = buffer;
            }
        }

        #endregion
    }
}