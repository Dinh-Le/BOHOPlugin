using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOHO.Core.Entities
{
    public class BOHOConfiguration
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int MilestoneId { get; set; }
    }
}
