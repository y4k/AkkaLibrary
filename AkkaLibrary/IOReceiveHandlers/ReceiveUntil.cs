using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Actor;
using NETCoreAsio.DataStructures;
using NETCoreAsio.Interfaces;

namespace AkkaLibrary.IOReceiveHandlers
{
    public class ReceiveUntilActor : ReceiveActor
    {
        public ReceiveUntilActor(string name, string delimiter) : this(name, Encoding.ASCII.GetBytes(delimiter))
        {
        }

        public ReceiveUntilActor(string name, byte delimiter) : this(name, new []{ delimiter })
        {
            //Create match condition
            _matchCondition = (start, end) =>
            {
                for (var i = start; i < end; ++i)
                {
                    if (_buffer[i] == delimiter)
                    {
                        return new Tuple<int, bool>(i + 1, true);
                    }
                }
                return new Tuple<int, bool>(end, false);
            };
        }

        public ReceiveUntilActor(string name, byte[] delimiter) : this(name)
        {
            _matchCondition = (start, end) =>
            {
                var length = delimiter.Length;
                for (var i = start; i <= end - length; ++i)
                {
                    var match = true;
                    for (var j = 0; j < length; ++j)
                    {
                        if (_buffer[i + j] != delimiter[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        return new Tuple<int, bool>(i + length, true);
                    }
                }
                return new Tuple<int, bool>(Math.Max(0, end - length), false);
            };
        }

        protected ReceiveUntilActor(string name) : base()
        {
            _buffer = new CircularQueue<byte>(10000, true);                        
        }

        private void Ready()
        {
            Receive<IOReadResult>(msg =>
            {
                foreach (var b in msg.ReadResult)
                {
                    _buffer.Add(b);
                }

                if (_buffer.Count > 0)
                {
                    var match = false;
                    
                    do
                    {
                        var result = _matchCondition(_matchOffset, _buffer.Count);
                        if (result.Item2)
                        {
                            //Success with offset 0
                            Sender.Tell(result.Item1);
                            match = true;
                        }
                        else if (_buffer.Count == _buffer.Capacity)
                        {
                            //Error. No buffer space available. Drop the buffer.
                            _buffer.Clear();
                            match = false;
                        }
                        else
                        {
                            //Normally request another read here. Wait until data arrives and retain the offset from the match condition
                            _matchOffset = result.Item1;
                            match = false;
                        }
                    } while (match);                
                }
                else
                {
                    //Request more data from the port. 0 is the match offset.
                    _matchOffset = 0;
                }
            });
        }

        private int _matchOffset;
        private MatchCondition _matchCondition;
        private readonly CircularQueue<byte> _buffer;

        public static Props GetProps(string name, string delimiter) => GetProps(name, Encoding.ASCII.GetBytes(delimiter));

        public static Props GetProps(string name, byte[] delimiter) => Props.Create(() => new ReceiveUntilActor(name, delimiter));

        #region Messages

        public sealed class IOReadResult
        {
            public IReadOnlyList<byte> ReadResult { get; }

            public IOReadResult(IEnumerable<byte> result)
            {
                ReadResult = result.ToArray();
            }
        }

        #endregion
    }
}