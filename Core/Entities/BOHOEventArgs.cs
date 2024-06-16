using System.Collections.Generic;

namespace BOHO.Core.Entities
{
    public class BOHOEventArgs
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public int PresetId { get; set; }
        public List<BoundingBoxInfo> BoundingBoxes { get; set; }
    }
}
