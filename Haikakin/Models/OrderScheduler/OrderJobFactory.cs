using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.OrderScheduler
{
    public class OrderJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public OrderJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        => ActivatorUtilities.CreateInstance(_serviceProvider, bundle.JobDetail.JobType) as IJob;
        public void ReturnJob(IJob job)
        {
            if (job is IDisposable disposableJob)
            {
                disposableJob.Dispose();
            }
        }
    }
}
