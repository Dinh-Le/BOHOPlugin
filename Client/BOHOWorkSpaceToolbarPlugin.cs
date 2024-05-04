using System;
using System.Collections.Generic;
using VideoOS.Platform;
using VideoOS.Platform.Client;

namespace BOHO.Client
{
    internal class BOHOWorkSpaceToolbarPluginInstance : WorkSpaceToolbarPluginInstance
    {
        private Item _window;

        public BOHOWorkSpaceToolbarPluginInstance()
        {
        }

        public override void Init(Item window)
        {
            _window = window;

            Title = "BOHO";
        }

        public override void Activate()
        {
            // Here you should put whatever action that should be executed when the toolbar button is pressed
        }

        public override void Close()
        {
        }

    }

    internal class BOHOWorkSpaceToolbarPlugin : WorkSpaceToolbarPlugin
    {
        public BOHOWorkSpaceToolbarPlugin()
        {
        }

        public override Guid Id
        {
            get { return BOHODefinition.BOHOWorkSpaceToolbarPluginId; }
        }

        public override string Name
        {
            get { return "BOHO"; }
        }

        public override void Init()
        {
            // TODO: remove below check when BOHODefinition.BOHOWorkSpaceToolbarPluginId has been replaced with proper GUID
            if (Id == new Guid("22222222-2222-2222-2222-222222222222"))
            {
                System.Windows.MessageBox.Show("Default GUID has not been replaced for BOHOWorkSpaceToolbarPluginId!");
            }

            WorkSpaceToolbarPlaceDefinition.WorkSpaceIds = new List<Guid>() { ClientControl.LiveBuildInWorkSpaceId, ClientControl.PlaybackBuildInWorkSpaceId, BOHODefinition.BOHOWorkSpacePluginId };
            WorkSpaceToolbarPlaceDefinition.WorkSpaceStates = new List<WorkSpaceState>() { WorkSpaceState.Normal };
        }

        public override void Close()
        {
        }

        public override WorkSpaceToolbarPluginInstance GenerateWorkSpaceToolbarPluginInstance()
        {
            return new BOHOWorkSpaceToolbarPluginInstance();
        }
    }
}
