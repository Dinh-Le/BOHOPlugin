using System.Collections.Generic;

namespace BOHO.Core.Entities
{
    public class Node
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Device> Devices { get; set; }
    }
}
