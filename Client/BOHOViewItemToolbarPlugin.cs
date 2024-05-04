using System;
using System.Collections.Generic;
using System.Drawing;
using BOHO.Application.ViewModel;
using BOHO.Core;
using VideoOS.Platform;
using VideoOS.Platform.Client;

namespace BOHO.Client
{
    internal class BOHOViewItemToolbarPluginInstance : ViewItemToolbarPluginInstance
    {
        private Item _viewItemInstance;
        private Item _window;
        private ViewItemToolbarPluginViewModel _vm;

        public override void Init(Item viewItemInstance, Item window)
        {
            _viewItemInstance = viewItemInstance;
            _window = window;
            _vm = RootContainer.Get<ViewItemToolbarPluginViewModel>();
        }

        public override void Activate()
        {
        }

        public override void Close()
        {
        }

        public override ToolbarPluginWpfUserControl GenerateWpfUserControl()
        {
            var bohoRepository = RootContainer.Get<Core.Interfaces.IBOHORepository>();
            _vm.Nodes = bohoRepository.Nodes;
            return new ViewItemToolbarPluginWpfUserControl(_vm)
            {
                WindowFQID = _window.FQID,
                ViewItemInstanceFQID = _viewItemInstance.FQID,
            };
        }
    }

    internal class BOHOViewItemToolbarPlugin : ViewItemToolbarPlugin
    {
    
        public override Guid Id
        {
            get { return BOHODefinition.BOHOViewItemToolbarPluginId; }
        }

        public override string Name
        {
            get { return "BOHO"; }
        }
       
        public override ToolbarPluginOverflowMode ToolbarPluginOverflowMode
        {
            get { return ToolbarPluginOverflowMode.AsNeeded; }
        }

        public override ToolbarPluginType ToolbarPluginType
        {
            get { return ToolbarPluginType.UserControl; }
        }

        public BOHOViewItemToolbarPlugin()
        {
        }

        public override void Init()
        {
            ViewItemToolbarPlaceDefinition.ViewItemIds = new List<Guid>() { BOHODefinition.BOHOViewItemPlugin };
            ViewItemToolbarPlaceDefinition.WorkSpaceIds = new List<Guid>() { ClientControl.LiveBuildInWorkSpaceId, ClientControl.PlaybackBuildInWorkSpaceId };
            ViewItemToolbarPlaceDefinition.WorkSpaceStates = new List<WorkSpaceState>() { WorkSpaceState.Normal };
        }

        public override void Close()
        {
        }

        public override ViewItemToolbarPluginInstance GenerateViewItemToolbarPluginInstance()
        {
            return new BOHOViewItemToolbarPluginInstance();
        }
    }
}
