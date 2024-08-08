using BOHO.Application.ViewModel;

namespace BOHO.Client
{
    /// <summary>
    /// Interaction logic for ViewItemToolbarPluginWpfUserControl.xaml
    /// </summary>
    public partial class ViewItemToolbarPluginWpfUserControl
        : VideoOS.Platform.Client.ToolbarPluginWpfUserControl
    {
        public ViewItemToolbarPluginWpfUserControl(ViewItemToolbarPluginViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
