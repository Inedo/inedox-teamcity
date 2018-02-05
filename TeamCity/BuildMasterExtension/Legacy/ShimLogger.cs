using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class ShimLogger : ILogSink
    {
        private ILogger logger;
        private ShimLogger(ILogger logger) => this.logger = logger;

        public static ShimLogger Create(ILogger logger)
        {
            if (logger == null)
                return null;
            else
                return new ShimLogger(logger);
        }

        public void Log(IMessage message) => this.logger.Log(message.Level, message.Message);
    }
}
