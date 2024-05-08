using System;
using BOHO.Application.ViewModel;
using BOHO.Client;
using BOHO.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

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

            var serviceCollection = new ServiceCollection()
                .AddSingleton<EventListener>()
                .AddSingleton(
                    new Entities.BOHOConfiguration
                    {
                        IP = "192.168.100.14",
                        ApiPort = 5500,
                        WebPort = 8081,
                        Username = "root",
                        Password = "Goback@2021",
                        MilestoneId = 6
                    }
                )
                .AddSingleton<Interfaces.IBOHORepository, BOHORepository>()
                // Views
                .AddTransient<BOHOWorkSpaceViewItemWpfUserControl>()
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
