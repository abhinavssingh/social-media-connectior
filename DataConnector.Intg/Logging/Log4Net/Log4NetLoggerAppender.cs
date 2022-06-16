using log4net.Appender;
using log4net.Core;
using Microsoft.Azure.WebJobs.Host;

namespace DataConnector.Intg.Logging.Log4Net
{
    public class Log4NetLoggerAppender : AppenderSkeleton
    {
        private readonly TraceWriter logger;

        public Log4NetLoggerAppender(TraceWriter logger)
        {
            this.logger = logger;
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            switch (loggingEvent.Level.Name)
            {
                case "DEBUG":
                    this.logger.Verbose($"{loggingEvent.LoggerName} - {loggingEvent.RenderedMessage}");
                    break;
                case "INFO":
                    this.logger.Info($"{loggingEvent.RenderedMessage}");
                    break;
                case "WARN":
                    this.logger.Warning($"{loggingEvent.LoggerName} - {loggingEvent.RenderedMessage}");
                    break;
                case "ERROR":
                    this.logger.Error($"{loggingEvent.LoggerName} - {loggingEvent.RenderedMessage}");
                    break;
                case "FATAL":
                    this.logger.Error($"{loggingEvent.LoggerName} - {loggingEvent.RenderedMessage}");
                    break;
                default:
                    this.logger.Info($"{loggingEvent.LoggerName} - {loggingEvent.RenderedMessage}");
                    break;
            }
        }
    }
}
