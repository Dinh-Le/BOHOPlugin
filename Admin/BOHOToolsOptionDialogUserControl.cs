using VideoOS.Platform.Admin;

namespace BOHO.Admin
{
    public partial class BOHOToolsOptionDialogUserControl : ToolsOptionsDialogUserControl
    {
        public BOHOToolsOptionDialogUserControl()
        {
            InitializeComponent();
        }

        public override void Init() { }

        public override void Close() { }

        public string MyPropValue
        {
            set { textBoxPropValue.Text = value ?? ""; }
            get { return textBoxPropValue.Text; }
        }
    }
}
