using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOHO.Client;
using BOHO.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;

namespace BOHO.Application.ViewModel
{
    public partial class ViewItemToolbarPluginViewModel : ObservableObject, IDisposable
    {
        private bool boundingBoxEnabled;
        public bool BoundingBoxEnabled
        {
            get => boundingBoxEnabled;
            set => SetProperty(ref boundingBoxEnabled, value);
        }

        private bool ruleEnabled;
        public bool RuleEnabled
        {
            get => ruleEnabled;
            set => SetProperty(ref ruleEnabled, value);
        }

        private bool ruleNameEnabled;
        public bool RuleNameEnabled
        {
            get => ruleNameEnabled;
            set => SetProperty(ref ruleNameEnabled, value);
        }

        private Core.Entities.Device selectedDevice;
        public Core.Entities.Device SelectedDevice
        {
            get => selectedDevice;
            set => SetProperty(ref selectedDevice, value);
        }

        private List<Core.Entities.Node> nodes;
        public List<Core.Entities.Node> Nodes
        {
            get => nodes;
            set => SetProperty(ref nodes, value);
        }

        private bool ptzEnabled;
        public bool PtzEnabled
        {
            get => ptzEnabled;
            set => SetProperty(ref ptzEnabled, value);
        }

        public FQID WindowFQID { set; get; }
        public FQID ViewItemInstanceFQID { set; get; }

        private readonly IBOHORepository _bohoRepository;
        private readonly IMessageService _messageService;
        private object _deviceStatusHandleId;

        public ViewItemToolbarPluginViewModel(
            IBOHORepository bohoRepository,
            IMessageService messageService
        )
        {
            this.Nodes = new List<Core.Entities.Node>();
            this.RuleEnabled = false;
            this.BoundingBoxEnabled = false;
            this.RuleNameEnabled = false;
            this.PtzEnabled = false;
            this.SelectedDevice = new Core.Entities.Device { ID = -1, Name = "Chọn camera" };
            this._bohoRepository = bohoRepository;
            this._messageService = messageService;
        }

        private object OnDeviceStatusChanged(Message message, FQID sender, FQID related)
        {
            PtzEnabled = (bool)message.Data;
            return null;
        }

        [RelayCommand()]
        private async Task SelectDevice(
            Core.Entities.Device device,
            CancellationToken cancellationToken
        )
        {
            if (device.ID == this.SelectedDevice.ID)
            {
                return;
            }

            this.SelectedDevice = device;

            if (this._deviceStatusHandleId != null)
            {
                EnvironmentManager.Instance.UnRegisterReceiver(this._deviceStatusHandleId);
            }

            var topic = $"/device/{device.ID}/status";
            this._deviceStatusHandleId = EnvironmentManager.Instance.RegisterReceiver(
                OnDeviceStatusChanged,
                new MessageIdFilter(topic)
            );

            var message = new Message("/device")
            {
                Data = new SetDeviceMessage
                {
                    Device = device,
                    ViewItemInstanceFQID = ViewItemInstanceFQID,
                    WindowFQID = WindowFQID
                }
            };
            var handlerTasks = EnvironmentManager
                .Instance.SendMessage(message)
                .Where(obj => obj is Task)
                .Select(task => (Task)task);
            foreach (var task in handlerTasks)
            {
                await task;
            }
        }

        [RelayCommand(CanExecute = nameof(CanChangeDeviceServiceStatus))]
        private async Task ChangeDeviceServiceStatus(CancellationToken cancellationToken)
        {
            try
            {
                if (this.PtzEnabled)
                {
                    await this._bohoRepository.StartService(this.SelectedDevice);
                }
                else
                {
                    await this._bohoRepository.StopService(this.SelectedDevice);
                }
            }
            catch
            {
                string errorMessage = this.PtzEnabled
                    ? "Start touring by BOHO failed"
                    : "Pause touring by BOHO failed";
                this._messageService.ShowError("Error", errorMessage);

                this.PtzEnabled = !this.PtzEnabled;
            }
        }

        private bool CanChangeDeviceServiceStatus()
        {
            return this.SelectedDevice.ID > 0;
        }

        [RelayCommand(CanExecute = nameof(CanChangeBoundingBoxVisibility))]
        private void ChangeBoundingBoxVisibility()
        {
            EnvironmentManager.Instance.SendMessage(
                new Message($"/device/{this.SelectedDevice.ID}/bounding_visibility")
                {
                    Data = this.BoundingBoxEnabled
                }
            );
        }

        private bool CanChangeBoundingBoxVisibility()
        {
            return this.SelectedDevice.ID > 0;
        }

        [RelayCommand(CanExecute = nameof(CanChangeRuleVisibility))]
        private async Task ChangeRuleVisibility()
        {
            var tasks = EnvironmentManager
                .Instance.SendMessage(
                    new Message($"/device/{this.SelectedDevice.ID}/rule_visibility")
                    {
                        Data = this.RuleEnabled
                    }
                )
                .Where(task => task is Task);
            foreach (var task in tasks)
            {
                await (Task)task;
            }
        }

        private bool CanChangeRuleVisibility()
        {
            return this.SelectedDevice.ID > 0;
        }

        [RelayCommand(CanExecute = nameof(CanChangeRuleNameVisibility))]
        private void ChangeRuleNameVisibility()
        {
            EnvironmentManager.Instance.SendMessage(
                new Message($"/device/{this.SelectedDevice.ID}/rule_name_visibility")
                {
                    Data = this.RuleNameEnabled
                }
            );
        }

        private bool CanChangeRuleNameVisibility()
        {
            return this.SelectedDevice.ID > 0;
        }

        public void Dispose()
        {
            if (this._deviceStatusHandleId is not null)
            {
                EnvironmentManager.Instance.UnRegisterReceiver(this._deviceStatusHandleId);
                this._deviceStatusHandleId = null;
            }
        }
    }
}
