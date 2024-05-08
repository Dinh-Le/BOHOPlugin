using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using BOHO.Admin;
using BOHO.Background;
using BOHO.Client;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.Background;
using VideoOS.Platform.Client;

namespace BOHO
{
    /// <summary>
    /// The PluginDefinition is the ‘entry’ point to any plugin.
    /// This is the starting point for any plugin development and the class MUST be available for a plugin to be loaded.
    /// Several PluginDefinitions are allowed to be available within one DLL.
    /// Here the references to all other plugin known objects and classes are defined.
    /// The class is an abstract class where all implemented methods and properties need to be declared with override.
    /// The class is constructed when the environment is loading the DLL.
    /// </summary>
    public class BOHODefinition : PluginDefinition
    {
        private static System.Drawing.Image _treeNodeImage;
        private static System.Drawing.Image _topTreeNodeImage;

        internal static Guid BOHOPluginId = new Guid("c2aace01-b635-498d-9096-a6859ca9c891");
        internal static Guid BOHOKind = new Guid("6fe5354b-1c83-4ef5-bfc2-843604ad4e92");
        internal static Guid BOHOSidePanel = new Guid("c8d3210c-9245-416a-8b77-6a5ae094994b");
        internal static Guid BOHOViewItemPlugin = new Guid("0d5bbc00-2066-41c3-84c9-62d7291fe96b");
        internal static Guid BOHOSettingsPanel = new Guid("95f459e1-448e-46e3-878e-74250e868c6e");
        internal static Guid BOHOBackgroundPlugin = new Guid(
            "e9308807-4a1b-4332-a682-f9e72e6fe931"
        );
        internal static Guid BOHOWorkSpacePluginId = new Guid(
            "aca22cbe-3640-4a58-a281-bc6817097365"
        );
        internal static Guid BOHOWorkSpaceViewItemPluginId = new Guid(
            "0d55096c-9280-4802-8fb9-e5abd51da265"
        );
        internal static Guid BOHOTabPluginId = new Guid("a04f71dd-afed-4bb5-9612-fb0e41db332a");
        internal static Guid BOHOViewLayoutId = new Guid("9f1acb8c-017d-4e03-916c-c91bb2c6c21d");

        // IMPORTANT! Due to shortcoming in Visual Studio template the below cannot be automatically replaced with proper unique GUIDs, so you will have to do it yourself
        internal static Guid BOHOWorkSpaceToolbarPluginId = new Guid(
            "41184ed3-9e6a-4d9d-aec4-1314d4fbe1db"
        );
        internal static Guid BOHOViewItemToolbarPluginId = new Guid(
            "c3fa0fb3-ab9e-4174-90f3-4c70ca37d036"
        );
        internal static Guid BOHOToolsOptionDialogPluginId = new Guid(
            "3ad00ba3-87c0-4761-8a69-43c5878519f3"
        );

        #region Private fields

        private UserControl _treeNodeInofUserControl;

        //
        // Note that all the plugin are constructed during application start, and the constructors
        // should only contain code that references their own dll, e.g. resource load.

        private List<BackgroundPlugin> _backgroundPlugins = new List<BackgroundPlugin>();
        private Collection<SettingsPanelPlugin> _settingsPanelPlugins =
            new Collection<SettingsPanelPlugin>();
        private List<ViewItemPlugin> _viewItemPlugins = new List<ViewItemPlugin>();
        private List<ItemNode> _itemNodes = new List<ItemNode>();
        private List<SidePanelPlugin> _sidePanelPlugins = new List<SidePanelPlugin>();
        private List<String> _messageIdStrings = new List<string>();
        private List<SecurityAction> _securityActions = new List<SecurityAction>();
        private List<WorkSpacePlugin> _workSpacePlugins = new List<WorkSpacePlugin>();
        private List<TabPlugin> _tabPlugins = new List<TabPlugin>();
        private List<ViewItemToolbarPlugin> _viewItemToolbarPlugins =
            new List<ViewItemToolbarPlugin>();
        private List<WorkSpaceToolbarPlugin> _workSpaceToolbarPlugins =
            new List<WorkSpaceToolbarPlugin>();
        private List<ViewLayout> _viewLayouts = new List<ViewLayout> { new BOHOViewLayout() };
        private List<ToolsOptionsDialogPlugin> _toolsOptionsDialogPlugins =
            new List<ToolsOptionsDialogPlugin>();

        #endregion

        #region Initialization

        /// <summary>
        /// Load resources
        /// </summary>
        static BOHODefinition()
        {
            _treeNodeImage = Properties.Resources.DummyItem;
            _topTreeNodeImage = Properties.Resources.Server;
        }

        /// <summary>
        /// Get the icon for the plugin
        /// </summary>
        internal static Image TreeNodeImage
        {
            get { return _treeNodeImage; }
        }

        #endregion

        /// <summary>
        /// This method is called when the environment is up and running.
        /// Registration of Messages via RegisterReceiver can be done at this point.
        /// </summary>
        public override void Init()
        {
            // Populate all relevant lists with your plugins etc.
            _itemNodes.Add(
                new ItemNode(
                    BOHOKind,
                    Guid.Empty,
                    "BOHO",
                    _treeNodeImage,
                    "BOHOs",
                    _treeNodeImage,
                    Category.Text,
                    true,
                    ItemsAllowed.Many,
                    new BOHOItemManager(BOHOKind),
                    null
                )
            );

            if (EnvironmentManager.Instance.EnvironmentType == EnvironmentType.SmartClient)
            {
                _workSpacePlugins.Add(new BOHOWorkSpacePlugin());
                //_sidePanelPlugins.Add(new BOHOSidePanelPlugin());
                _viewItemPlugins.Add(new BOHOViewItemPlugin());
                _viewItemPlugins.Add(new BOHOWorkSpaceViewItemPlugin());
                _viewItemToolbarPlugins.Add(new BOHOViewItemToolbarPlugin());
                //_workSpaceToolbarPlugins.Add(new BOHOWorkSpaceToolbarPlugin());
                //_settingsPanelPlugins.Add(new BOHOSettingsPanelPlugin());
            }

            if (EnvironmentManager.Instance.EnvironmentType == EnvironmentType.Administration)
            {
                _tabPlugins.Add(new BOHOTabPlugin());
                _toolsOptionsDialogPlugins.Add(new BOHOToolsOptionDialogPlugin());
            }

            _backgroundPlugins.Add(new BOHOBackgroundPlugin());

            Core.RootContainer.Initialize();
        }

        /// <summary>
        /// The main application is about to be in an undetermined state, either logging off or exiting.
        /// You can release resources at this point, it should match what you acquired during Init, so additional call to Init() will work.
        /// </summary>
        public override void Close()
        {
            _itemNodes.Clear();
            _sidePanelPlugins.Clear();
            _viewItemPlugins.Clear();
            _settingsPanelPlugins.Clear();
            _backgroundPlugins.Clear();
            _workSpacePlugins.Clear();
            _tabPlugins.Clear();
            _viewItemToolbarPlugins.Clear();
            _workSpaceToolbarPlugins.Clear();
            _toolsOptionsDialogPlugins.Clear();
        }

        /// <summary>
        /// Return any new messages that this plugin can use in SendMessage or PostMessage,
        /// or has a Receiver set up to listen for.
        /// The suggested format is: "YourCompany.Area.MessageId"
        /// </summary>
        public override List<string> PluginDefinedMessageIds
        {
            get { return _messageIdStrings; }
        }

        /// <summary>
        /// If authorization is to be used, add the SecurityActions the entire plugin
        /// would like to be available.  E.g. Application level authorization.
        /// </summary>
        public override List<SecurityAction> SecurityActions
        {
            get { return _securityActions; }
            set { }
        }

        #region Identification Properties

        /// <summary>
        /// Gets the unique id identifying this plugin component
        /// </summary>
        public override Guid Id
        {
            get { return BOHOPluginId; }
        }

        /// <summary>
        /// This Guid can be defined on several different IPluginDefinitions with the same value,
        /// and will result in a combination of this top level ProductNode for several plugins.
        /// Set to Guid.Empty if no sharing is enabled.
        /// </summary>
        public override Guid SharedNodeId
        {
            get { return Guid.Empty; }
        }

        /// <summary>
        /// Define name of top level Tree node - e.g. A product name
        /// </summary>
        public override string Name
        {
            get { return "BOHO"; }
        }

        /// <summary>
        /// Your company name
        /// </summary>
        public override string Manufacturer
        {
            get { return "Your Company name"; }
        }

        /// <summary>
        /// Version of this plugin.
        /// </summary>
        public override string VersionString
        {
            get { return "1.0.0.0"; }
        }

        /// <summary>
        /// Icon to be used on top level - e.g. a product or company logo
        /// </summary>
        public override System.Drawing.Image Icon
        {
            get { return _topTreeNodeImage; }
        }

        #endregion


        #region Administration properties

        /// <summary>
        /// A list of server side configuration items in the administrator
        /// </summary>
        public override List<ItemNode> ItemNodes
        {
            get { return _itemNodes; }
        }

        /// <summary>
        /// An extension plug-in running in the Administrator to add a tab for built-in devices and hardware.
        /// </summary>
        public override ICollection<TabPlugin> TabPlugins
        {
            get { return _tabPlugins; }
        }

        /// <summary>
        /// An extension plug-in running in the Administrator to add more tabs to the Tools-Options dialog.
        /// </summary>
        public override List<ToolsOptionsDialogPlugin> ToolsOptionsDialogPlugins
        {
            get { return _toolsOptionsDialogPlugins; }
        }

        /// <summary>
        /// A user control to display when the administrator clicks on the top TreeNode
        /// </summary>
        public override UserControl GenerateUserControl()
        {
            _treeNodeInofUserControl = new HelpPage();
            return _treeNodeInofUserControl;
        }

        /// <summary>
        /// This property can be set to true, to be able to display your own help UserControl on the entire panel.
        /// When this is false - a standard top and left side is added by the system.
        /// </summary>
        public override bool UserControlFillEntirePanel
        {
            get { return false; }
        }
        #endregion

        #region Client related methods and properties

        /// <summary>
        /// A list of Client side definitions for Smart Client
        /// </summary>
        public override List<ViewItemPlugin> ViewItemPlugins
        {
            get { return _viewItemPlugins; }
        }

        /// <summary>
        /// An extension plug-in running in the Smart Client to add more choices on the Settings panel.
        /// Supported from Smart Client 2017 R1. For older versions use OptionsDialogPlugins instead.
        /// </summary>
        public override Collection<SettingsPanelPlugin> SettingsPanelPlugins
        {
            get { return _settingsPanelPlugins; }
        }

        /// <summary>
        /// An extension plugin to add to the side panel of the Smart Client.
        /// </summary>
        public override List<SidePanelPlugin> SidePanelPlugins
        {
            get { return _sidePanelPlugins; }
        }

        /// <summary>
        /// Return the workspace plugins
        /// </summary>
        public override List<WorkSpacePlugin> WorkSpacePlugins
        {
            get { return _workSpacePlugins; }
        }

        /// <summary>
        /// An extension plug-in to add to the view item toolbar in the Smart Client.
        /// </summary>
        public override List<ViewItemToolbarPlugin> ViewItemToolbarPlugins
        {
            get { return _viewItemToolbarPlugins; }
        }

        /// <summary>
        /// An extension plug-in to add to the work space toolbar in the Smart Client.
        /// </summary>
        public override List<WorkSpaceToolbarPlugin> WorkSpaceToolbarPlugins
        {
            get { return _workSpaceToolbarPlugins; }
        }

        /// <summary>
        /// An extension plug-in running in the Smart Client to provide extra view layouts.
        /// </summary>
        public override List<ViewLayout> ViewLayouts
        {
            get { return _viewLayouts; }
        }

        #endregion


        /// <summary>
        /// Create and returns the background task.
        /// </summary>
        public override List<BackgroundPlugin> BackgroundPlugins
        {
            get { return _backgroundPlugins; }
        }
    }
}
