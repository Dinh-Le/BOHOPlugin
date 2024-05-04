using System.Collections.Generic;
using System.Threading.Tasks;
using BOHO.Core.Entities;

namespace BOHO.Core.Interfaces
{
    public interface IBOHORepository
    {
        List<Node> Nodes { get; set; }

        Task Login();
        Task Synchronize();
        Task StartService(Device device);
        Task StopService(Device device);
        Task<bool> GetServiceStatus(Device device);
        Task<IEnumerable<Rule>> GetRules(Device device);
    }
}
