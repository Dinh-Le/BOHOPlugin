using VideoOS.Platform.Client;

namespace BOHO.Client
{
    public class BOHOWorkSpaceViewItemManager : ViewItemManager
    {
        public BOHOWorkSpaceViewItemManager() : base("BOHOWorkSpaceViewItemManager")
        {
        }

        public override ViewItemWpfUserControl GenerateViewItemWpfUserControl()
        {
            return new BOHOWorkSpaceViewItemWpfUserControl();
        }
    }
}
