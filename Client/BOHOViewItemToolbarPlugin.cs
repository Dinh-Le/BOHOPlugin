using System;
using BOHO.Application.ViewModel;
using BOHO.Core;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Client.Export;

namespace BOHO.Client
{
    internal class BOHOViewItemToolbarPluginInstance(ViewItemToolbarPluginViewModel viewModel) : ViewItemToolbarPluginInstance
    {
        private readonly ViewItemToolbarPluginViewModel _viewModel = viewModel;

        public override void Init(Item viewItemInstance, Item window)
        {
            _viewModel.ViewItemInstanceFQID = viewItemInstance.FQID;
            _viewModel.WindowFQID = window.FQID;

            _viewModel.Init(viewItemInstance.GetViewItemProperties());
        }

        public override ToolbarPluginWpfUserControl GenerateWpfUserControl()
            => new ViewItemToolbarPluginWpfUserControl(_viewModel);

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
            => RootContainer.Get<BOHOViewItemToolbarPluginInstance>();
    }
}
