using Autofac;
using Autofac.Core;
using log4net;
using log4net.Config;
using Microsoft.Azure.WebJobs.Host;
using System.Linq;
using System.Reflection;

namespace DataConnector.Intg.Logging.Log4Net
{
    public class Log4NetLoggingModule : Autofac.Module
    {
        public Log4NetLoggingModule(TraceWriter logger)
        {
          //  var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());

            /*This will load the Configuration Settings of the Log4Net Appender in the BasicConfigurator of Log4Net in the way
            which is different than the way of web.config or app.config through which we load Log4Net Configuration Settings in other Web Applications*/
            BasicConfigurator.Configure(new Log4NetLoggerAppender(logger));
        }

        private void InjectLoggerProperties(object instance)
        {
            var instanceType = instance.GetType();

            // Get all the injectable properties to set.
            // If you wanted to ensure the properties were only UNSET properties,
            // here's where you'd do it.
            var properties = instanceType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(ILog) && p.CanWrite && p.GetIndexParameters().Length == 0);

            // Set the properties located.
            foreach (var propToSet in properties)
            {
                propToSet.SetValue(instance, LogManager.GetLogger(instanceType), null);
            }
        }

        private void OnComponentPreparing(object sender, PreparingEventArgs e)
        {
            e.Parameters = e.Parameters.Union(
                new[]
                {
                    new ResolvedParameter(
                        (p, i) => p.ParameterType == typeof(ILog),
                        (p, i) => LogManager.GetLogger(p.Member.DeclaringType)
                    ),

                });
        }

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            // Handle constructor parameters.
            registration.Preparing += OnComponentPreparing;

            // Handle properties.
            registration.Activated += (sender, e) => InjectLoggerProperties(e.Instance);
        }
    }
}
