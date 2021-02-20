using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace PerformanceServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var exitCode = HostFactory.Run(x =>
            {
                x.Service<PerformanceMonitor>(s =>
                {
                    s.ConstructUsing(performanceMonitor => new PerformanceMonitor());
                    s.WhenStarted(performanceMonitor => performanceMonitor.Run());
                    s.WhenStopped(performanceMonitor => performanceMonitor.Stop());
                });

                x.RunAsLocalSystem();
                x.SetServiceName("Performance Monitor");
                x.SetDisplayName("Performance Monitor");
                x.SetDescription("Performance Monitor for TV monitoring");
            });

            Environment.ExitCode = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
        }
    }
}
