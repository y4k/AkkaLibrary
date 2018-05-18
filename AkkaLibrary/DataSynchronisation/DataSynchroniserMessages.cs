namespace AkkaLibrary.DataSynchronisation
{
    public static class DataSynchroniserMessages
    {
        public sealed class Configure
        {
            public DataSyncConfiguration Config { get; }

            public Configure(DataSyncConfiguration config)
            {
                Config = config;
            }
        }
    }
}