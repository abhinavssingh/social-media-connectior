using Autofac;
using DataConnector.Intg.SocialMedia.Common;
using DataConnector.Intg.SocialMedia.Communicator;
using Microsoft.Azure.WebJobs.Host;
using System;

namespace DataConnector.Intg.Logging
{
    /// <summary>
    /// Basic Class to inject Dependency into the Class Library's Different Classes for Logging Mechanisms
    /// </summary>
    public static class Dependency
    {
        public static IContainer Container { get; private set; }
        public static void CreateContainer<T>(TraceWriter logger) where T : Autofac.Module
        {
            if (Container == null)
            {
                var builder = new ContainerBuilder();
                builder.RegisterType<ApplicationSettings>().As<ApplicationSettings>();
                builder.RegisterType<FileConvertor>().As<FileConvertor>();
                builder.RegisterType<SocialHelper>().As<SocialHelper>();
                builder.RegisterType<AzureOperationsService>().As<AzureOperationsService>();
                builder.RegisterType<FaceBookDataCommunicator>().As<FaceBookDataCommunicator>();
                builder.RegisterType<FaceBookApiCommunicator>().As<FaceBookApiCommunicator>();
                builder.RegisterType<GoogleApiCommunicator>().As<GoogleApiCommunicator>();
                builder.RegisterType<GoogleDataCommunicator>().As<GoogleDataCommunicator>();
                builder.RegisterType<TwitterDataCommunicator>().As<TwitterDataCommunicator>();
                builder.RegisterType<TwitterApiCommunicator>().As<TwitterApiCommunicator>();
                builder.RegisterType<Dv360DataCommunicator>().As<Dv360DataCommunicator>();
                builder.RegisterType<Dv360ApiCommunicator>().As<Dv360ApiCommunicator>();
                builder.RegisterType<SimilarWebDataCommunicator>().As<SimilarWebDataCommunicator>();
                builder.RegisterType<SimilarWebApiCommunicator>().As<SimilarWebApiCommunicator>();
                builder.RegisterType<GoogleTireDataCommunicator>().As<GoogleTireDataCommunicator>();                
                builder.RegisterType<KeyVaultService>().As<KeyVaultService>();

                builder.RegisterModule((Autofac.Core.IModule)Activator.CreateInstance(typeof(T), new object[] { logger }));

                Container = builder.Build();
            }
        }
    }
}
