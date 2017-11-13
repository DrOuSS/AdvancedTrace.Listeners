using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AdvancedTraceLib;
using AdvancedTraceListeners.Xml;
using NUnit.Framework;

namespace AdvancedTraceListenersTest.Xml
{
    public class XmlWriterTraceListenerWithDelayedTest
    {
        private readonly string _currentDirectory;

        public XmlWriterTraceListenerWithDelayedTest()
        {
            _currentDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DateTime.Today.ToString("yyyy-MM-dd"));
        }

        [Test]
        public void Check1TraceInformationWithDelayed()
        {
            CleanOutput();

            using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory))
            {
                AdvancedTrace.AddTraceListener(AdvancedTrace.ListenerType.All, logStorage);
                AdvancedTrace.TraceInformation("Information", "Info");

                Thread.Sleep(31000);

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(Path.Combine(_currentDirectory, "Working_session_1.xml"));

                AdvancedTrace.RemoveTraceListener(AdvancedTrace.ListenerType.All, logStorage);
            }
        }

        [Test]
        public void StressTrace10ThreadAnd10000TraceWithDelayed()
        {
            CleanOutput();

            System.Diagnostics.Debug.WriteLine("DirectoryCreated");

            using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory))
            {
                AdvancedTrace.AddTraceListener(AdvancedTrace.ListenerType.All, logStorage);
                var tasks = new List<Task>();

                System.Diagnostics.Debug.WriteLine("Listener added");

                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                for (var i = 0; i < 10; i++)
                {
                    var i1 = i;
                    tasks.Add(Task.Factory.StartNew(() =>
                                                    {
                                                        for (var j = 0; j < 10000; j++)
                                                            AdvancedTrace.TraceInformation("MyInformation " + i1 + " " + j, "Info");
                                                    }));
                }

                logStorage.Flush();

                Task.WaitAll(tasks.ToArray());

                System.Diagnostics.Debug.WriteLine("Tasks finished added");

                stopWatch.Stop();
                var ts = stopWatch.Elapsed;
                System.Diagnostics.Debug.WriteLine("Time Execute Trace :" + $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");

                Thread.Sleep(31000);

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(Path.Combine(_currentDirectory, "Working_session_1.xml"));
            }
        }

        private void CleanOutput()
        {
            if (Directory.Exists(_currentDirectory))
                Directory.Delete(_currentDirectory, true);
        }
    }
}