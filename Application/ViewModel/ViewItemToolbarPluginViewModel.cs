using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BOHO.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;

namespace BOHO.Application.ViewModel
{
    public partial class ViewItemToolbarPluginViewModel : ObservableObject
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

        private readonly IBOHORepository _bohoRepository;

        public ViewItemToolbarPluginViewModel(IBOHORepository bohoRepository)
        {
            this.Nodes = new List<Core.Entities.Node>();
            this.RuleEnabled = false;
            this.BoundingBoxEnabled = false;
            this.RuleNameEnabled = false;
            this.PtzEnabled = false;
            this.SelectedDevice = new Core.Entities.Device { ID = -1, Name = "Chọn camera" };
            this._bohoRepository = bohoRepository;
        }

        [RelayCommand(CanExecute = nameof(CanChangeDeviceServiceStatus))]
        private async Task ChangeDeviceServiceStatus(CancellationToken cancellationToken)
        {
            try
            {
                if (this.PtzEnabled)
                {
                    await this._bohoRepository.StopService(this.SelectedDevice);
                }
                else
                {
                    await this._bohoRepository.StartService(this.SelectedDevice);
                }
            }
            catch
            {
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
                    Data = this.RuleEnabled
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
            EnvironmentManager.Instance.SendMessage(
                new Message($"/device/{this.SelectedDevice.ID}/rule_visibility")
                {
                    Data = this.RuleEnabled
                }
            );
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
                    Data = this.RuleEnabled
                }
            );
        }

        private bool CanChangeRuleNameVisibility()
        {
            return this.SelectedDevice.ID > 0;
        }
    }
}
