using System;
using System.IO;

namespace AdvancedTraceListenersTest.Xml
{
    public class XmlWriterTraceListenerTest
    {
        protected readonly string CurrentDirectory;

        protected XmlWriterTraceListenerTest()
        {
            CurrentDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DateTime.Today.ToString("yyyy-MM-dd"));
        }

        protected void CleanOutput()
        {
            if (Directory.Exists(CurrentDirectory))
                Directory.Delete(CurrentDirectory, true);
        }
    }
}