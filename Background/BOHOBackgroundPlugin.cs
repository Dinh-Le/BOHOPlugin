using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOHO.Core;
using BOHO.Core.Interfaces;
using VideoOS.Platform;
using VideoOS.Platform.Background;

namespace BOHO.Background
{
    /// <summary>
    /// A background plugin will be started during application start and be running until the user logs off or application terminates.<br/>
    /// The Environment will call the methods Init() and Close() when the user login and logout,
    /// so the background task can flush any cached information.<br/>
    /// The base class implementation of the LoadProperties can get a set of configuration,
    /// e.g. the configuration saved by the Options Dialog in the Smart Client or a configuration set saved in one of the administrators.
    /// Identification of which configuration to get is done via the GUID.<br/>
    /// The SaveProperties method can be used if updating of configuration is relevant.
    /// <br/>
    /// The configuration is stored on the server the application is logged into, and should be refreshed when the ApplicationLoggedOn method is called.
    /// Configuration can be user private or shared with all users.<br/>
    /// <br/>
    /// This plugin could be listening to the Message with MessageId == Server.ConfigurationChangedIndication to when when to reload its configuration.
    /// This event is send by the environment within 60 second after the administrator has changed the configuration.
    /// </summary>
    public class BOHOBackgroundPlugin(IBOHORepository bohoRepo, IEventListener eventListener)
        : BackgroundPlugin
    {
        private readonly IBOHORepository _bohoRepo = bohoRepo;
        private readonly IEventListener _eventListener = eventListener;
        private readonly CancellationTokenSource _stoppingTokenSource = new();

        /// <summary>
        /// Gets the unique id identifying this plugin component
        /// </summary>
        public override Guid Id
        {
            get { return BOHODefinition.BOHOBackgroundPlugin; }
        }

        /// <summary>
        /// The name of this background plugin
        /// </summary>
        public override String Name
        {
            get { return "BOHO BackgroundPlugin"; }
        }

        /// <summary>
        /// Called by the Environment when the user has logged in.
        /// </summary>
        public override void Init()
        {
            Task.Run(
                () => ExecuteAsync(_stoppingTokenSource.Token),
                this._stoppingTokenSource.Token
            );
        }

        /// <summary>
        /// Called by the Environment when the user log's out.
        /// You should close all remote sessions and flush cache information, as the
        /// user might logon to another server next time.
        /// </summary>
        public override void Close()
        {
            this._stoppingTokenSource.Cancel();
        }

        /// <summary>
        /// Define in what Environments the current background task should be started.
        /// </summary>
        public override List<EnvironmentType> TargetEnvironments
        {
            get { return [EnvironmentType.SmartClient]; }
        }

        /// <summary>
        /// the thread doing the work
        /// </summary>
        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await this._eventListener.InitializeAsync();

            await this._bohoRepo.Login();

            await this._bohoRepo.Synchronize();

            while (!cancellationToken.IsCancellationRequested)
            {
                var devices = this._bohoRepo.Nodes.SelectMany(node => node.Devices).ToList();

                foreach (var device in devices)
                {
                    try
                    {
                        var status = await this._bohoRepo.GetServiceStatus(device);
                        device.ServiceStatus = status;

                        var messageId = $"/device/{device.ID}/status";
                        EnvironmentManager.Instance?.SendMessage(
                            new VideoOS.Platform.Messaging.Message(messageId) { Data = status }
                        );
                    }
                    catch
                    {
                        // ignore
                    }
                }

                await Task.Delay(3000, cancellationToken);
            }
        }
    }
}
