using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOHO.Application.Models;
using BOHO.Client;
using BOHO.Core;
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

        private bool _boundingBoxCheckboxVisible = false;
        public bool BoundingBoxCheckboxVisible
        {
            get => _boundingBoxCheckboxVisible;
            set => SetProperty(ref _boundingBoxCheckboxVisible, value);
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

            // Do not render the bounding box for PTZ camera
            this.BoundingBoxCheckboxVisible = !this.SelectedDevice.IsPTZ;

            if (this._deviceStatusHandleId != null)
            {
                EnvironmentManager.Instance.UnRegisterReceiver(this._deviceStatusHandleId);
            }

            IEnumerable<Core.Entities.Rule> rules = await this._bohoRepository.GetRules(
                this.SelectedDevice
            );

            var topic = $"/device/{device.ID}/status";
            this._deviceStatusHandleId = EnvironmentManager.Instance.RegisterReceiver(
                OnDeviceStatusChanged,
                new MessageIdFilter(topic)
            );

            var message = new Message("/device")
            {
                Data = new SetDeviceEventArgs
                {
                    Device = device,
                    Rules = rules,
                    ViewItemInstanceFQID = ViewItemInstanceFQID,
                    WindowFQID = WindowFQID
                }
            };

            EnvironmentManager.Instance.SendMessage(message);
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
        private void ChangeRuleVisibility()
        {
            var tasks = EnvironmentManager
                .Instance.SendMessage(
                    new Message($"/device/{this.SelectedDevice.ID}/rule_visibility")
                    {
                        Data = this.RuleEnabled
                    }
                )
                .OfType<Task>()
                .ToArray();

            Task.WaitAll(tasks);
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
