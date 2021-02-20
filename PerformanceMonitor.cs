using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;
using SuperSocket.WebSocket;
using Newtonsoft.Json;
using Microsoft.VisualBasic.Devices;

namespace PerformanceServer
{
    class PerformanceMonitor
    {
        private WebSocketServer oWsServer;
        private int nWsPort;
        private ConcurrentDictionary<WebSocketSession, Timer> aClientTimers = new ConcurrentDictionary<WebSocketSession, Timer>();
        readonly PerformanceCounter oCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        readonly PerformanceCounter oMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        public object oPerfocmanceLock;

        public PerformanceMonitor()
        {
            oPerfocmanceLock = new object();
        }

        public PerformanceMonitor(object lockObject)
        {
            oPerfocmanceLock = lockObject;
        }

        public void Run(int port = 8080)
        {
            oWsServer = new WebSocketServer();
            if (!oWsServer.Setup(port))
            {
                Console.WriteLine("Websocket Server Error setup on :" + nWsPort);
                return;
            }

            nWsPort = port;
            oWsServer.NewSessionConnected += NewSessionConnected;
            oWsServer.SessionClosed += SessionClosed;

            aClientTimers.Clear();

            if (oWsServer.Start())
            {
                Console.WriteLine("Websocket Server started on :" + nWsPort);
            }

            //var performanceCounterCategories = PerformanceCounterCategory.GetCategories()
            //    .FirstOrDefault(category => category.CategoryName == "Processor");
            //var performanceCounters = performanceCounterCategories.GetCounters("_Total");
            //Console.WriteLine("Displaying performance counters for Memory category:--\n");
            //foreach (PerformanceCounter performanceCounter in performanceCounters)
            //{
            //    Console.WriteLine(performanceCounter.CounterName);
            //}
        }

        private void SendPerformanceDetails(object sender, ElapsedEventArgs e, WebSocketSession session)
        {
            if (session.Connected)
            {
                float nTotalMemory = GetTotalMemoryInMBytes();
                float nCpuUsagePercent = 0;
                float nMemFreeMB = 0;
                float nMemUseMB = 0; ;

                lock (oPerfocmanceLock)
                {
                    nCpuUsagePercent = oCpuCounter.NextValue();
                    nMemFreeMB = oMemoryCounter.NextValue();
                    nMemUseMB = nTotalMemory-nMemFreeMB;
                }

                string data = JsonConvert.SerializeObject(new PerformanceDetails
                {
                    Date = DateTime.Now,
                    CPU = nCpuUsagePercent,
                    MemFree = nMemFreeMB,
                    MemUsed = nMemUseMB,
                });
                session.Send(data);
            }
            else
            {
                Console.WriteLine("Disconnected event handler");
            }
        }

        private void SetTimer(WebSocketSession session)
        {
            var timer = new Timer(1000);
            timer.Elapsed += (sender, e) => SendPerformanceDetails(sender, e, session);
            timer.AutoReset = true;

            while (!aClientTimers.TryAdd(session, timer)) ;

            timer.Start();
            Console.WriteLine("Timer started for peer " + session.Host + " (" + session.StartTime + ")");
        }

        public void Stop()
        {
            foreach (var item in aClientTimers)
            {
                item.Value.Stop();
            }
            oWsServer.Stop();
            Console.WriteLine("Websocket Server stopped on :" + nWsPort);
        }

        private void SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            Console.WriteLine("Session [" + session.Host + "] closed due to reason " + value + "(start: " + session.StartTime + ")");

            aClientTimers[session].Stop();
            aClientTimers.TryRemove(session, out _);
        }

        private void NewSessionConnected(WebSocketSession session)
        {
            Console.WriteLine("Session [" + session.Host + "] opened successfully at " + session.StartTime);
            SetTimer(session);
        }

        static ulong GetTotalMemoryInMBytes()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024 * 1024);
        }
    }
}
