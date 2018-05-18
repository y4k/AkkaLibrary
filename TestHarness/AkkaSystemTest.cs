using System;
using System.Threading;
using Serilog;
using AkkaLibrary.ServiceScaffold;
using AkkaLibrary.Common.Configuration;

namespace TestHarness
{
    public static class AkkaSystemTest
    {
        private static void Run()
        {
            Log.Logger = new LoggerConfiguration()
                            .WriteTo.Console(outputTemplate:"[{Level:u3}] [{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message}{NewLine}{Exception}")
                            .CreateLogger();

            Console.WriteLine("AkkaLibrary Test Harness.");

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            var pluginSystem = PluginSystemFactory.NewPluginSystem("TestActorSystem", CommonConfigs.BasicConfig());

            var registry = pluginSystem.PluginRegistryRef;

            var system = pluginSystem.System;
            //===================================================

            var config = new TestPluginConfiguration
            {
                Name = "test-plugin"
            };

            var testPlugin = pluginSystem.CreatePlugin(config);

            //===================================================
            Console.WriteLine("Press Ctrl+C to terminate.");
            exitEvent.WaitOne();

            var terminateTask = system.Terminate();

            
            var success = terminateTask.Wait(TimeSpan.FromSeconds(5));


            Log.Information($"Actor system terminated {(success ? "successfully" : "unsuccessfully")}.");

            Log.CloseAndFlush();
        }
    }
}
