using System;
using BOHO.Application.ViewModel;
using BOHO.Core;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Client.Export;

namespace BOHO.Client
{
    internal class BOHOViewItemToolbarPluginInstance : ViewItemToolbarPluginInstance
    {
        private Item _viewItemInstance;
        private Item _window;

        public override void Init(Item viewItemInstance, Item window)
        {
            this._viewItemInstance = viewItemInstance;
            this._window = window;
        }

        public override ToolbarPluginWpfUserControl GenerateWpfUserControl()
        {
            ViewItemToolbarPluginViewModel viewModel =
                RootContainer.Get<ViewItemToolbarPluginViewModel>();

            viewModel.ViewItemInstanceFQID = this._viewItemInstance.FQID;
            viewModel.WindowFQID = this._window.FQID;

            viewModel.Init(this._viewItemInstance.GetViewItemProperties());

            return new ViewItemToolbarPluginWpfUserControl(viewModel);
        }

        public override void Close() { }
    }

    internal class BOHOViewItemToolbarPlugin : ViewItemToolbarPlugin
    {
        public override Guid Id => BOHODefinition.BOHOViewItemToolbarPluginId;

        public override string Name => "BOHO View Item Toolbar";

        public override ToolbarPluginOverflowMode ToolbarPluginOverflowMode =>
            ToolbarPluginOverflowMode.NeverInOverflow;

        public override ToolbarPluginType ToolbarPluginType => ToolbarPluginType.UserControl;

        public override void Init()
        {
            ViewItemToolbarPlaceDefinition.ViewItemIds = [BOHODefinition.BOHOViewItemPlugin];
            ViewItemToolbarPlaceDefinition.WorkSpaceIds = [ClientControl.LiveBuildInWorkSpaceId];
            ViewItemToolbarPlaceDefinition.WorkSpaceStates = [WorkSpaceState.Normal];
        }

        public override void Close() { }

        public override ViewItemToolbarPluginInstance GenerateViewItemToolbarPluginInstance()
        {
            return new BOHOViewItemToolbarPluginInstance();
        }
    }
}
