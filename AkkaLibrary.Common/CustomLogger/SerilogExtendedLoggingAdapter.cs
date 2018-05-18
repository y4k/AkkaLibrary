using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Event;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace AkkaLibrary.Common.CustomLogger
{
    public class SerilogExtendedLoggingAdapter : ILoggingAdapter
    {
        private readonly ILoggingAdapter _adapter;
        private readonly ContextNode _enricherNode;

        public SerilogExtendedLoggingAdapter(ILoggingAdapter adapter) : this(adapter, null) { }

        private SerilogExtendedLoggingAdapter(ILoggingAdapter adapter, ContextNode enricherNode)
        {
            _adapter = adapter;
            _enricherNode = enricherNode;
        }

        public bool IsDebugEnabled => _adapter.IsDebugEnabled;

        public bool IsInfoEnabled => _adapter.IsInfoEnabled;

        public bool IsWarningEnabled => _adapter.IsWarningEnabled;

        public bool IsErrorEnabled => _adapter.IsErrorEnabled;
        
        public bool IsEnabled(LogLevel logLevel) => _adapter.IsEnabled(logLevel);

        public void Debug(string format, params object[] args)
        {
            _adapter.Debug(format, BuildArgs(args));
        }

        public void Error(string format, params object[] args)
        {
            _adapter.Error(format, BuildArgs(args));
        }

        public void Error(Exception cause, string format, params object[] args)
        {
            _adapter.Error(cause, format, BuildArgs(args));
        }

        public void Info(string format, params object[] args)
        {
            _adapter.Debug(format, BuildArgs(args));
        }

        public void Warning(string format, params object[] args)
        {
            _adapter.Warning(format, BuildArgs(args));
        }

        public void Log(LogLevel logLevel, string format, params object[] args)
        {
            _adapter.Log(logLevel, format, BuildArgs(args));
        }

        public ILoggingAdapter SetContextProperty(string name, object value, bool destructureObjects = false)
        {
            var contextProperty = new PropertyEnricher(name, value, destructureObjects);

            var contextNode = new ContextNode
            {
                Enricher = contextProperty,
                Next = _enricherNode
            };

            return new SerilogExtendedLoggingAdapter(_adapter, contextNode);
        }

        private object[] BuildArgs(IEnumerable<object> args)
        {
            var newArgs = args.ToList();
            if (_enricherNode != null)
            {
                var currentNode = _enricherNode;
                while (currentNode != null)
                {
                    newArgs.Add(currentNode.Enricher);
                    currentNode = currentNode.Next;
                }
            }
            return newArgs.ToArray();
        }

        private class ContextNode
        {
            public ContextNode Next { get; set; }
            public ILogEventEnricher Enricher { get; set; }
        }
    }
}