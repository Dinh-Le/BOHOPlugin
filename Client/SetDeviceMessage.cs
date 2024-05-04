using VideoOS.Platform;

namespace BOHO.Client
{
    public class SetDeviceMessage
    {
        public Core.Entities.Device Device { get; set; }
        public FQID WindowFQID { get; set; }
        public FQID ViewItemInstanceFQID { get; set; }
    }
}
