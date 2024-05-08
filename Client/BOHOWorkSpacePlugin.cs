using System;
using VideoOS.Platform.Client;

namespace BOHO.Client
{
    public class BOHOWorkSpacePlugin : WorkSpacePlugin
    {
        /// <summary>
        /// The Id.
        /// </summary>
        public override Guid Id
        {
            get { return BOHODefinition.BOHOWorkSpacePluginId; }
        }

        /// <summary>
        /// The name displayed on top
        /// </summary>
        public override string Name
        {
            get { return "BOHO"; }
        }

        /// <summary>
        /// We do not support setup mode
        /// </summary>
        public override bool IsSetupStateSupported
        {
            get { return false; }
        }

        public override void Init()
        {
            LoadProperties(true);

            ViewAndLayoutItem.Layout = [new(000, 000, 1000, 800)];
            ViewAndLayoutItem.Name = Name;

            ViewAndLayoutItem.InsertViewItemPlugin(0, new BOHOWorkSpaceViewItemPlugin(), new());
        }

        public override void Close() { }
    }
}
