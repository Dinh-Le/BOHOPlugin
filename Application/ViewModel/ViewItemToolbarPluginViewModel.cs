using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOHO.Application.Models;
using BOHO.Application.Util;
using BOHO.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoOS.Platform;
using VideoOS.Platform.Client.Export;
using VideoOS.Platform.Messaging;

namespace BOHO.Application.ViewModel
{
    public partial class ViewItemToolbarPluginViewModel : ObservableObject, IDisposable
    {
        private bool _boundingBoxEnabled;
        public bool BoundingBoxEnabled
        {
            get => _boundingBoxEnabled;
            set => SetProperty(ref _boundingBoxEnabled, value);
        }

        private bool _boundingBoxCheckboxVisible = false;
        public bool BoundingBoxCheckboxVisible
        {
            get => _boundingBoxCheckboxVisible;
            set => SetProperty(ref _boundingBoxCheckboxVisible, value);
        }

        private bool _ruleEnabled;
        public bool RuleEnabled
        {
            get => _ruleEnabled;
            set => SetProperty(ref _ruleEnabled, value);
        }

        private bool _ruleNameEnabled;
        public bool RuleNameEnabled
        {
            get => _ruleNameEnabled;
            set => SetProperty(ref _ruleNameEnabled, value);
        }

        private Core.Entities.Device _selectedDevice = new() { ID = -1, Name = "Chọn camera" };
        public Core.Entities.Device SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        private List<Core.Entities.Node> _nodes;
        public List<Core.Entities.Node> Nodes
        {
            get => _nodes;
            set => SetProperty(ref _nodes, value);
        }

        private bool _ptzEnabled;
        public bool PtzEnabled
        {
            get => _ptzEnabled;
            set => SetProperty(ref _ptzEnabled, value);
        }

        public FQID WindowFQID { get; set; }
        public FQID ViewItemInstanceFQID { get; set; }

        private readonly IBOHORepository _bohoRepository;
        private readonly IMessageService _messageService;
        private object _deviceStatusHandleId;

        public ViewItemToolbarPluginViewModel(
            IBOHORepository bohoRepository,
            IMessageService messageService
        )
        {
            this._bohoRepository = bohoRepository;
            this._messageService = messageService;
        }

        public void Init(Dictionary<string, string> properties)
        {
            properties.TryGetValue("rule_visible", out string str);
            this.RuleEnabled = str.ToBool();

            properties.TryGetValue("rule_name_visible", out str);
            this.RuleNameEnabled = str.ToBool();

            properties.TryGetValue("bounding_box_visible", out str);
            this.BoundingBoxEnabled = str.ToBool();

            properties.TryGetValue("selected_device", out str);
            this.SelectedDevice =
                str.Deserialize<Core.Entities.Device>() ?? new() { ID = -1, Name = "Chọn camera" };

            this.Nodes = this._bohoRepository.Nodes;
        }

        private object OnDeviceStatusChanged(Message message, FQID sender, FQID related)
        {
            PtzEnabled = (bool)message.Data;
            return null;
        }

        [RelayCommand]
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
