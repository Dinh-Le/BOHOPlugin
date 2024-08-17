using System;
using BOHO.Application.Util;
using BOHO.Application.ViewModel;
using BOHO.Background;
using BOHO.Client;
using BOHO.Core.Interfaces;
using BOHO.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace BOHO.Core
{
    public static class RootContainer
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize()
        {
            if (_serviceProvider != null)
            {
                throw new InvalidOperationException("The root container has been intialized");
            }

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    "logs/log.txt",
                    LogEventLevel.Debug,
                    rollingInterval: RollingInterval.Hour,
                    retainedFileTimeLimit: TimeSpan.FromDays(1),
                    flushToDiskInterval: TimeSpan.FromSeconds(30)
                )
                .Destructure.ByTransforming<object>((value) => JsonConvert.SerializeObject(value))
                .CreateLogger();

            var serviceCollection = new ServiceCollection()
                .AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true))
                .AddSingleton<IEventListener, EventListener>()
                .AddSingleton(
                    new Entities.BOHOConfiguration
                    {
                        IP = "192.168.100.14",
                        ApiPort = 5500,
                        WebPort = 8081,
                        Username = "root",
                        Password = "Goback@2021",
                        MilestoneId = 6,
                        MqttTopic = "service-communicate",
                        MqttHost = "192.168.100.14",
                        MqttPort = 1883,
                        AnalyticImageWidth = 1920,
                        AnalyticImageHeight = 1080
                    }
                )
                .AddSingleton<IBOHORepository, BOHORepository>()
                // Views
                .AddTransient<BOHOViewItemManager>()
                .AddTransient<BOHOBackgroundPlugin>()
                .AddTransient<BOHOWorkSpaceViewItemWpfUserControl>()
                .AddSingleton<IMessageService, MessageService>()
                // View models
                .AddTransient<ViewItemToolbarPluginViewModel>()
                .AddTransient<BOHOWorkspaceViewItemWpfViewModel>()
                .AddHttpClient();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public static T Get<T>()
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(
                    "The root container has not been intialized yet"
                );
            }

            return _serviceProvider.GetRequiredService<T>();
        }
    }
}
