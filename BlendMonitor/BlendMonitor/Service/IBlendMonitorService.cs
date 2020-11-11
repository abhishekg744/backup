using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendMonitor.Service
{
    public interface IBlendMonitorService
    {
        Task<int> ProcessBlenders();
    }
}
