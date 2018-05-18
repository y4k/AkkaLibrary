using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using AkkaLibrary.Common.Utilities;
using AkkaLibrary.IOReceiveHandlers;
using NETCoreAsio.DataStructures;
using NETCoreAsio.Interfaces;

namespace AkkaLibrary.Serial
{
    public class SerialPortSupervisor : ReceiveActor
    {
        public SerialPortSupervisor(string name)
        {
            _logger = Context.WithIdentity(name);
        }

        private void Ready()
        {
            Receive<OpenNewSerialPort>(msg =>
            {
                var self = Self;
                var name = $"port-handler-{msg.PortName}";
                var handler = Context.ActorOf(SerialPortReadActor.GetProps(name, self), name);
                _portHandlers.Add(msg.PortName, handler);
            });

            Receive<ReceiveUntilOnPort>(msg =>
            {
                var self = Self;
                var name = $"receive-until-handler-{msg.PortName}";
                var handler = Context.ActorOf(ReceiveUntilActor.GetProps(name, "!"), name);

                if (_portDataReceivers.ContainsKey(msg.PortName))
                {
                    _portDataReceivers[msg.PortName].Add(handler);
                }
                else
                {
                    _portDataReceivers.Add(msg.PortName, new List<IActorRef>{handler});
                }
            });

            Receive<int>(msg =>
            {
                _logger.Warning($"Bytes:{msg}");
            });

            Receive<SerialPortReadActor.PortReadResult>(msg =>
            {
                if(_portDataReceivers.ContainsKey(msg.PortName))
                {
                    foreach (var actor in _portDataReceivers[msg.PortName])
                    {
                        actor.Tell(new ReceiveUntilActor.IOReadResult(msg.Buffer));
                    }
                }
            });
        }

        public Props GetProps(string name) => Props.Create<SerialPortSupervisor>(name);

        private Dictionary<string, IActorRef> _portHandlers = new Dictionary<string, IActorRef>();

        private Dictionary<string, List<IActorRef>> _portDataReceivers = new Dictionary<string, List<IActorRef>>();
        private readonly ILoggingAdapter _logger;

        #region Messages

        public sealed class OpenNewSerialPort
        {
            public string PortName { get; }
            public int BaudRate { get; }

            public OpenNewSerialPort(string portName, int baudRate)
            {
                PortName = portName;
                BaudRate = baudRate;
            }
        }

        public class ReceiveUntilOnPort
        {
            public string PortName { get; }
        }

        #endregion
    }
}