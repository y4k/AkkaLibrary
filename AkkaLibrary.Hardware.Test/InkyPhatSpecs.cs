using System;
using System.Threading;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using AkkaLibrary.Common.Logging;
using AkkaLibrary.Hardware.Exceptions;
using AkkaLibrary.Hardware.Managers;
using AkkaLibrary.Hardware.StaticWrappers;
using FluentAssertions;
using Moq;
using Serilog;
using Xunit;

namespace AkkaLibrary.Hardware.Test
{
    public class InkyPhatSpecs : TestKit
    {
        public InkyPhatSpecs()
        {
            Serilog.Log.Logger = LoggerFactory.Logger;
        }

        [Fact]
        public void InitialisedWithoutException()
        {
            var inkyController = Mock.Of<IInkyPhatController>
            (
                controller => controller.Initialise() == true
            );

            inkyController.Initialise().Should().BeTrue();

            var manager = Sys.ActorOf(Props.Create(() => new InkyPhatManager(inkyController)));
            
            manager.Should().NotBeNull().And.NotBe(ActorRefs.Nobody);
        }

        [Fact]
        public void FirstDrawAcceptedAndCompleted()
        {
            var pixels = new InkyPhatColours[InkyPhat.Width, InkyPhat.Height];

            var inkyController = Mock.Of<IInkyPhatController>( controller => controller.Initialise() == true && controller.Draw(pixels) == true);

            inkyController.Initialise().Should().BeTrue();
            inkyController.Draw(pixels).Should().BeTrue();

            var manager = Sys.ActorOf(Props.Create(() => new InkyPhatManager(inkyController)));
            
            manager.Should().NotBeNull().And.NotBe(ActorRefs.Nobody);

            manager.Tell(new InkyPhatManager.Draw(pixels), TestActor);

            ExpectMsg<InkyPhatManager.DrawAccepted>().DrawId.Should().Be(0);
            ExpectMsg<InkyPhatManager.DrawComplete>().DrawId.Should().Be(0);
        }

        [Fact]
        public void DrawAcceptedButSubsequentRejected()
        {
            var inkyControllerMock = new Mock<IInkyPhatController>();

            var pixels = new InkyPhatColours[InkyPhat.Width, InkyPhat.Height];

            inkyControllerMock.Setup(x => x.Initialise()).Returns(true);
            inkyControllerMock.Setup(x => x.Draw(pixels)).Returns(true);
            inkyControllerMock.Setup(x => x.Draw(pixels)).Returns(() =>
            {
                Thread.Sleep(5000);
                return true;
            });

            var inkyController = inkyControllerMock.Object;

            inkyController.Initialise().Should().BeTrue();

            var manager = Sys.ActorOf(Props.Create(() => new InkyPhatManager(inkyController)));
            
            manager.Should().NotBeNull().And.NotBe(ActorRefs.Nobody);

            manager.Tell(new InkyPhatManager.Draw(pixels), TestActor);

            ExpectMsg<InkyPhatManager.DrawAccepted>().DrawId.Should().Be(0);

            manager.Tell(new InkyPhatManager.Draw(pixels), TestActor);
            manager.Tell(new InkyPhatManager.Draw(pixels), TestActor);
            manager.Tell(new InkyPhatManager.Draw(pixels), TestActor);

            ExpectMsg<InkyPhatManager.DrawRejected>().DrawId.Should().Be(0);
            ExpectMsg<InkyPhatManager.DrawRejected>().DrawId.Should().Be(0);
            ExpectMsg<InkyPhatManager.DrawRejected>().DrawId.Should().Be(0);

            ExpectMsg<InkyPhatManager.DrawComplete>(TimeSpan.FromSeconds(10)).DrawId.Should().Be(0);
        }
    }
}
