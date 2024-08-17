using System;
using System.Collections.Generic;

namespace BOHO.Core.Entities;

public class BoundingBox
{
    public int TrackingNumber { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string ObjectName { get; set; }
    public DateTime Timestamp { get; set; }
}

public class BOHOEventArgs
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; }
    public IEnumerable<BoundingBox> BoundingBoxes { get; set; }
}
