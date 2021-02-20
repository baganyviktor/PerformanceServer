using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceServer
{
    class PerformanceDetails
    {
        public float CPU { get; set; }
        public float MemFree { get; set; }
        public float MemUsed { get; internal set; }
        public DateTime Date { get; internal set; }
    }
}
