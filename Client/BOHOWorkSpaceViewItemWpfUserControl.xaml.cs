using System;
using BOHO.Application.ViewModel;
using VideoOS.Platform.Client;

namespace BOHO.Client
{
    /// <summary>
    /// Interaction logic for BOHOWorkSpaceViewItemWpfUserControl.xaml
    /// </summary>
    public partial class BOHOWorkSpaceViewItemWpfUserControl : ViewItemWpfUserControl
    {
        public BOHOWorkSpaceViewItemWpfUserControl(BOHOWorkspaceViewItemWpfViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }

        public override void Init() { }

        public override void Close() { }

        /// <summary>
        /// Do not show the sliding toolbar!
        /// </summary>
        public override bool ShowToolbar
        {
            get { return false; }
        }

        private void ViewItemWpfUserControl_ClickEvent(object sender, EventArgs e)
        {
            FireClickEvent();
        }

        private void ViewItemWpfUserControl_DoubleClickEvent(object sender, EventArgs e)
        {
            FireDoubleClickEvent();
        }
    }
}
