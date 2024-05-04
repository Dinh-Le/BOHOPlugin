using BOHO.Application.ViewModel;
using VideoOS.Platform;

namespace BOHO.Client
{
    /// <summary>
    /// Interaction logic for ViewItemToolbarPluginWpfUserControl.xaml
    /// </summary>
    public partial class ViewItemToolbarPluginWpfUserControl
        : VideoOS.Platform.Client.ToolbarPluginWpfUserControl
    {
        public FQID WindowFQID
        {
            get => ((ViewItemToolbarPluginViewModel)DataContext).WindowFQID;
            set => ((ViewItemToolbarPluginViewModel)DataContext).WindowFQID = value;
        }

        public FQID ViewItemInstanceFQID
        {
            get => ((ViewItemToolbarPluginViewModel)DataContext).ViewItemInstanceFQID;
            set => ((ViewItemToolbarPluginViewModel)DataContext).ViewItemInstanceFQID = value;
        }

        public ViewItemToolbarPluginWpfUserControl(ViewItemToolbarPluginViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
