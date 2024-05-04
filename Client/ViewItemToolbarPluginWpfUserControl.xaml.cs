using System.Windows;
using System.Windows.Controls;
using BOHO.Application.ViewModel;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;

namespace BOHO.Client
{
    /// <summary>
    /// Interaction logic for ViewItemToolbarPluginWpfUserControl.xaml
    /// </summary>
    public partial class ViewItemToolbarPluginWpfUserControl
        : VideoOS.Platform.Client.ToolbarPluginWpfUserControl
    {
        public FQID WindowFQID { set; get; }

        public FQID ViewItemInstanceFQID { set; get; }

        private object _deviceStatusHandleId;

        public ViewItemToolbarPluginWpfUserControl(ViewItemToolbarPluginViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        public override void Close()
        {
            if (this._deviceStatusHandleId != null)
            {
                EnvironmentManager.Instance.UnRegisterReceiver(this._deviceStatusHandleId);
                this._deviceStatusHandleId = null;
            }
        }

        private void OnCameraSelected(object sender, RoutedEventArgs e)
        {
            var device = (e.OriginalSource as MenuItem).DataContext as Core.Entities.Device;
            if (device == null)
                return;

            if (this._deviceStatusHandleId != null)
            {
                EnvironmentManager.Instance.UnRegisterReceiver(this._deviceStatusHandleId);
            }

            this._deviceStatusHandleId = EnvironmentManager.Instance.RegisterReceiver(
                (message, _, _) =>
                {
                    var vm = (this.DataContext as ViewItemToolbarPluginViewModel);
                    vm.PtzEnabled = (bool)message.Data;

                    return null;
                },
                new MessageIdFilter($"/device/{device.ID}/status")
            );

            var vm = DataContext as ViewItemToolbarPluginViewModel;

            vm.SelectedDevice = device;
            var message = new VideoOS.Platform.Messaging.Message("SetDevice")
            {
                Data = new SetDeviceMessage
                {
                    Device = device,
                    WindowFQID = WindowFQID,
                    ViewItemInstanceFQID = ViewItemInstanceFQID
                }
            };
            EnvironmentManager.Instance.SendMessage(message);
        }
    }
}
