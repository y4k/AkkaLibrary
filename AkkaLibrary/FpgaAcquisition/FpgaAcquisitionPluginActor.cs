using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Akka.Actor;
using Akka.IO;
using Serilog;

namespace AkkaLibrary
{
    public class FpgaAcquisitionPluginActor : ReceiveActor, IWithUnboundedStash
    {
        public IStash Stash { get; set; }
        
        public FpgaAcquisitionPluginActor()
        {
            Ready();
        }

        private void Ready()
        {
            Receive<FpgaPluginMessages.Configure>(msg =>
            {
                Log.Information("Configuring Fpga Actor");
                ConfigureSelf(msg.Configuration);
                Become(Working);
            });
        }

        private void Working()
        {
            Log.Information("Fpga Actor working.");

            Receive<FpgaPluginMessages.Configure>(msg =>
            {
                Stash.Stash();
                ReConfigureSelf(msg.Configuration);
                Stash.UnstashAll();
            });

            ReceiveAny(msg =>
            {
                Log.Warning($"FPGA Plugin supervisor received:{msg}");
            });
        }

        private void ReConfigureSelf(FpgaAcquisitionConfiguration configuration)
        {
            if(ShouldReconfigureAssembler(configuration))
            {
                //Reconfigure assembler
                Context.Stop(_assembler);
                _assembler = CreateAssemblerChild(configuration);
            }
            
            if(ShouldReconfigureDelimiter(configuration))
            {
                //Reconfigure delimiter
                Context.Stop(_framing);
                _framing = CreateDelimiterChild(configuration, _assembler);
            }

            if(ShouldReconfigureConnection(configuration))
            {
                //Reconfigure connection
                Context.Stop(_connector);
                _connector = CreateConnectionChild(configuration, _framing);
            }
        }

        private bool ShouldReconfigureConnection(IFpgaConnectionConfig config)
        {
            var oldReceiverConfig = _config as IFpgaConnectionConfig;

            return config.IpAddress != oldReceiverConfig.IpAddress ||
                   config.Port != oldReceiverConfig.Port;
        }

        private bool ShouldReconfigureDelimiter(IFpgaFramingConfig config)
        {
            var oldReceiverConfig = _config as IFpgaFramingConfig;

            return config.Delimiter != oldReceiverConfig.Delimiter ||
                   config.MaxFrameLength != oldReceiverConfig.MaxFrameLength;
        }

        private bool ShouldReconfigureAssembler(IFpgaAssemblerConfig config)
        {
            var oldReceiverConfig = _config as IFpgaAssemblerConfig;

            return config.OutputTarget != oldReceiverConfig.OutputTarget ||
                   config.SamplesPerPacket != oldReceiverConfig.SamplesPerPacket ||
                   Enumerable.SequenceEqual(config.ChannelList, oldReceiverConfig.ChannelList);
        }

        private void ConfigureSelf(FpgaAcquisitionConfiguration config)
        {
            _config = config;
            _assembler = CreateAssemblerChild(config);
            _framing = CreateDelimiterChild(config, _assembler);
            _connector = CreateConnectionChild(config, _framing);
        }

        private IActorRef CreateConnectionChild(IFpgaConnectionConfig config, IActorRef framingActor)
        {
            _endPoint = new IPEndPoint(IPAddress.Parse(config.IpAddress), config.Port);
            _retryConnectionTimeout = config.RetryConnectionTimeout;

            var actor = Context.ActorOf(FpgaConnectionActor.GetProps(_endPoint, _retryConnectionTimeout, framingActor));
            Context.Watch(actor);
            return actor;
        }

        private IActorRef CreateDelimiterChild(IFpgaFramingConfig config, IActorRef assembler)
        {
            return Context.ActorOf(FpgaDelimitingActor.GetProps(config.Delimiter, config.MaxFrameLength, assembler));
        }

        private IActorRef CreateAssemblerChild(IFpgaAssemblerConfig config)
        {
            return Context.ActorOf(FpgaSampleAssemblerActor.GetProps(config.ChannelList, config.SamplesPerPacket, config.OutputTarget));
        }

        public static Props GetProps() => Props.Create(() => new FpgaAcquisitionPluginActor());

        private FpgaAcquisitionConfiguration _config;
        private IActorRef _connector;
        private IActorRef _framing;
        private IActorRef _assembler;
        private IPEndPoint _endPoint;
        private TimeSpan _retryConnectionTimeout;
    }
}
