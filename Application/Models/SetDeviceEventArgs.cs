using System.Collections.Generic;
using BOHO.Core.Entities;
using VideoOS.Platform;

namespace BOHO.Application.Models
{
    public class SetDeviceEventArgs
    {
        public Device Device { get; set; }
        public IEnumerable<Rule> Rules { get; set; }
        public FQID WindowFQID { get; set; }
        public FQID ViewItemInstanceFQID { get; set; }
    }
}
