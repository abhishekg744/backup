using BlendMonitor.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlendMonitor.Service
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IConfiguration _configuration;
        private IBlendMonitorRepository _blendMonitorRepo;
        private readonly IBlendMonitorService _blendMonitorService;
        private string programName;

        public TimedHostedService(IConfiguration configuration,
            IBlendMonitorRepository blendMonitorRepo, IBlendMonitorService blendMonitorService)
        {
            _configuration = configuration;
            _blendMonitorRepo = blendMonitorRepo;
            _blendMonitorService = blendMonitorService;
            programName = _configuration.GetSection("ProgramName").Value.ToUpper();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Blend Monitor is starting.");
            DateTime gDteCurTime = DateTime.Now;

            double? Data =  _blendMonitorRepo.GetCycleTime(programName);
            if (Data == null)
                Data = 3;
            int minutes = Convert.ToInt32(Data);
            Console.WriteLine("cycle time for Blend monitor - " + minutes + " minutes");
            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds((minutes * 60)));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            _blendMonitorService.ProcessBlenders();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Tank Monitor is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
