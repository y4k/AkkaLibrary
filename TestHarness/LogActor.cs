using Akka.Actor;
using Serilog;
using System.Collections;
using System.Text;
using AkkaLibrary;
using AkkaLibrary.Common.Objects;

namespace TestHarness
{
    public class LogActor : ReceiveActor
    {
        public LogActor()
        {
            Receive<IEnumerable>(msg =>
            {
                var stringBuilder = new StringBuilder();
                foreach (var item in msg)
                {
                    stringBuilder.Append($"{item},");
                }

                Log.Information(string.Join(",",stringBuilder.ToString()));
            });

            Receive<ChannelData<float>>(msg =>
            {
                var stringBuilder = new StringBuilder();
                foreach (var item in msg.Analogs)
                {
                    stringBuilder.Append($"[{item.Name},{item.Value}],");
                }
                foreach (var item in msg.Digitals)
                {
                    stringBuilder.Append($"[{item.Name},{item.Value}],");
                }

                Log.Information(string.Join(",",stringBuilder.ToString()));
            });

            ReceiveAny(msg =>
            {
                Log.Information(msg.ToString());
            });
        }
    }
}