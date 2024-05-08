using System;
using VideoOS.Platform.Client;

namespace BOHO.Client
{
    public class BOHOWorkSpaceViewItemPlugin : ViewItemPlugin
    {
        private static System.Drawing.Image _treeNodeImage;

        public BOHOWorkSpaceViewItemPlugin()
        {
            _treeNodeImage = Properties.Resources.WorkSpaceIcon;
        }

        public override Guid Id
        {
            get { return BOHODefinition.BOHOWorkSpaceViewItemPluginId; }
        }

        public override System.Drawing.Image Icon
        {
            get { return _treeNodeImage; }
        }

        public override string Name
        {
            get { return "BOHO Website"; }
        }

        public override bool HideSetupItem
        {
            get { return true; }
        }

        public override ViewItemManager GenerateViewItemManager()
        {
            return new BOHOWorkSpaceViewItemManager();
        }

        public override void Init() { }

        public override void Close() { }
    }
}
