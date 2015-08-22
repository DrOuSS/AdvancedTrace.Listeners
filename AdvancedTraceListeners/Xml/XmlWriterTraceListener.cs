using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AdvancedTraceLib;

namespace AdvancedTraceListeners.Xml
{
    
    public class XmlWriterTraceListener : AdvancedTraceListener
    {

        #region Constantes

        /// <summary>
        /// 5 s
        /// </summary>
        private const int TimerSaveFrequency = 30000;
        /// <summary>
        /// 0.1 s
        /// </summary>
        private const int WaitingBeforeWritingLogs = 100;

        #endregion

        #region Variables

        private readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
        private readonly Timer _timer;
        private DateTime _lastCreationLogDate;
        private readonly LogFileManager _logFileManager;
        private readonly ManualResetEventSlim _traceToWriteEmpty= new ManualResetEventSlim(true);
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private readonly Task _taskAppendLogsToDiskWithoutDelayed;
        
        #endregion

        #region Contructor / Destructor

        public XmlWriterTraceListener(string applicationName, string customLogPath = null, int historyDayCount = 2, bool isDelayedWrite = true)
        {
            CustomLogPath = customLogPath;
            IsDelayedWrite = isDelayedWrite;
            HistoryDayCount = historyDayCount;
            ApplicationName = applicationName;
            _lastCreationLogDate = DateTime.MinValue;

            _logFileManager = new LogFileManager(ApplicationName, BaseRootPath, historyDayCount);

            if (!IsDelayedWrite)
            {
                _taskAppendLogsToDiskWithoutDelayed = Task.Factory.StartNew(token =>
                    {

                        var cancellationToken = (CancellationToken) token;

                        while (true)
                        {
                            if (_lines.Count > 0)
                                WriteLogsToDisk();
                            else
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    break;

                                Thread.Sleep(WaitingBeforeWritingLogs);
                            }
                        }
                    }, _cancellationToken.Token, _cancellationToken.Token/*, TaskCreationOptions.LongRunning, TaskScheduler.Default*/);
            }
            else
                _timer = new Timer(OnTimerCallback, null, TimerSaveFrequency, TimerSaveFrequency);
        }

        ~XmlWriterTraceListener()
        {
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposing)
                return;

            _isDisposing = true;

            if (!IsDelayedWrite)
            {
                try
                {
                    _cancellationToken.Cancel();
                    _taskAppendLogsToDiskWithoutDelayed.Wait();
                    _taskAppendLogsToDiskWithoutDelayed.Dispose();
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch
                {

                }
            }
            else
            {
                try
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    _timer.Dispose();
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                    
                }
            }
            
            WriteLogsToDisk();

            if (_logFileManager!=null) _logFileManager.Dispose();

        }
        private volatile bool _isDisposing;

        #endregion

        #region Properties

        public string CurrentFilePath
        {
            get { return _logFileManager.CurrentFilePath; }
        }

        public string CustomLogPath
        {
            get { return _customLogPath; }
            private set { _customLogPath = value; }
        }
        private volatile string _customLogPath = string.Empty;

        public string ApplicationName
        {
            get { return _applicationName; }
            private set { _applicationName = value; }
        }
        private volatile string _applicationName = string.Empty;

        public int HistoryDayCount
        {
            get { return _historyDayCount; }
            private set { _historyDayCount = value; }
        }
        private volatile int _historyDayCount;

        public bool IsDelayedWrite
        {
            get { return _isDelayedWrite; }
            private set { _isDelayedWrite = value; }
        }
        private volatile bool _isDelayedWrite;

        public string BaseRootPath
        {
            get
            {

                if (string.IsNullOrWhiteSpace(_baseRootPath))
                {
                    string rootPath;

                    if (!string.IsNullOrEmpty(CustomLogPath))
                        rootPath = CustomLogPath;
                    else
                    {
                        rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ApplicationName);
                        rootPath = Path.Combine(rootPath, "Logs");
                    }
                    _baseRootPath = rootPath;
                }
                return _baseRootPath;
            }
        }
        private volatile string _baseRootPath = string.Empty;

        #endregion

        #region Advanced Trace Methods
        
        public override void WriteEx(string pstrType, object value, string userCategory = null, Exception exception = null)
        {
            WriteLineEx(pstrType, value, userCategory, exception);
        }

        public override void WriteLineEx(string pstrType, object value, string userCategory = null, Exception exception = null)
        {
            
            _traceToWriteEmpty.Reset();

            var formatedMessage = GetFormatedLog(pstrType, value.ToString(), exception, userCategory);

            _lines.Enqueue(formatedMessage);
        }
        
        #endregion

        #region Timer Callback

        private void OnTimerCallback(object state)
        {
            WriteLogsToDisk();
        }

        #endregion

        #region Log on disk management

        private void WriteLogsToDisk()
        {
            var stringBuilder = new StringBuilder();
            string traceToWrite;

            while (_lines.TryDequeue(out traceToWrite))
            {
                var date = DateTime.Now.Date;
                if (_lastCreationLogDate.Date < date)
                {
                    _lastCreationLogDate = date;
 
                    if (stringBuilder.Length > 0)
                    {
                        _logFileManager.Write(stringBuilder.ToString());

                        stringBuilder.Clear();
                    }

                    _logFileManager.CreateNewLogFile();

                    stringBuilder.Append(traceToWrite);
                }
                else
                    stringBuilder.Append(traceToWrite);
            }

            if (stringBuilder.Length > 0)
            {
                _logFileManager.Write(stringBuilder.ToString());

                stringBuilder.Clear();
            }

            _traceToWriteEmpty.Set();
        }

        public override void Flush()
        {
            base.Flush();


            if (!IsDelayedWrite)
                _traceToWriteEmpty.Wait();
            else
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

                WriteLogsToDisk();

                _timer.Change(TimerSaveFrequency, TimerSaveFrequency);
            }
            _logFileManager.Close();
        }

        #endregion

        #region Formated Log

        private string GetFormatedLog(string severity, string message, Exception exception, string userCategory)
        {
            var messageFormated = new StringBuilder();

            messageFormated.AppendFormat("<Log Time=\"{0}\" Severity=\"{1}\" UserCategory=\"{2}\" Message=\"{3}\"",
                                         DateTime.UtcNow.ToString("o"),
                                         severity ?? "",
                                         userCategory ?? "",
                                         HttpUtility.HtmlEncode(message) ?? "");

            if (exception != null)
                messageFormated.AppendFormat(">{0}</Log>", GetExceptionFormated(exception));
            else
                messageFormated.Append("/>");


            return messageFormated.ToString();
        }

        private string GetExceptionFormated(Exception exception)
        {
            if (exception == null)
                return string.Empty;

            var message = new StringBuilder();

            message.AppendFormat("<Exception Type=\"{0}\" Message=\"{1}\"",
                                 exception.GetType(),
                                 HttpUtility.HtmlEncode(exception.Message));

            if (exception.Source != null)
                message.AppendFormat(" Source=\"{0}\" ", HttpUtility.HtmlEncode(exception.Source));

            if (exception.StackTrace != null)
                message.AppendFormat(" StackTrace=\"{0}\" ", HttpUtility.HtmlEncode(exception.StackTrace));

            if (exception.InnerException != null)
                message.AppendFormat(">{0}</Exception>", GetExceptionFormated(exception.InnerException));
            else
                message.Append("/>");

            return message.ToString();
        }

        #endregion

        #region Class LogFileManager

        private class LogFileManager : IDisposable
        {
            private readonly Timer _closeTimer;
            private readonly object _fileStreamLocker;
            private readonly string _rootLogPath;
            private readonly int _onDiskLogHistoryDayCount;
            private FileStream _fileStream;
            private bool _disposed;
            private readonly Encoding _encoding;
            private readonly byte[] _closeXmlNodeBytes;

            private volatile string _applicationName = string.Empty;

            private const int CloseDelay = 200;

            #region Constructor / destructor

            public LogFileManager(string applicationName, string rootLogPath, int onDiskLogHistoryDayCount) : this(applicationName, rootLogPath, onDiskLogHistoryDayCount, Encoding.ASCII) { }

            public LogFileManager(string applicationName, string rootLogPath, int onDiskLogHistoryDayCount, Encoding encoding)
            {
                _applicationName = applicationName;
                _rootLogPath = rootLogPath;
                _onDiskLogHistoryDayCount = onDiskLogHistoryDayCount;

                if (onDiskLogHistoryDayCount <= 1)
                    throw new ArgumentException("Disk Log History Day Count is <= 1");

                _closeTimer = new Timer(OnClose, null, Timeout.Infinite, Timeout.Infinite);

                _fileStreamLocker = new object();

                _encoding = encoding;
                _closeXmlNodeBytes = _encoding.GetBytes("</Logs>");
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;

                    Close();

                    _closeTimer.Dispose();
                }
            }

            #endregion

            #region Properties

            public string CurrentFilePath
            {
                get { return _currentFilePath; }
                private set { _currentFilePath = value; }
            }
            private volatile string _currentFilePath = string.Empty;

            #endregion

            #region History management

            private void CleanDiskHistory()
            {
                var dateLimit = DateTime.Now.Date.AddDays(-_onDiskLogHistoryDayCount);

                if (Directory.Exists(_rootLogPath))
                {
                    var directoriesArray = Directory.GetDirectories(_rootLogPath);

                    foreach (var dirToCheck in directoriesArray)
                    {
                        try
                        {
                            if (Directory.GetCreationTime(dirToCheck).Date <= dateLimit)
                            {
                                if (Directory.GetFiles(dirToCheck, "Working_session_*.xml").Length > 0)
                                    Directory.Delete(dirToCheck, true);
                            }
                        }
                        catch (Exception error)
                        {
                            Debug.WriteLine("### Can't clean the directory :{0} error:{1}", dirToCheck, error);
                        }
                    }
                }
            }

            #endregion

            #region File management

            public void CreateNewLogFile()
            {
                string rootPath = Path.Combine(_rootLogPath, DateTime.Now.ToString("yyyy-MM-dd"));

                int sessionNumber = 1;

                lock (_fileStreamLocker)
                {
                    // Close any previous file if needed
                    Close();

                    if (!Directory.Exists(rootPath))
                    {
                        Directory.CreateDirectory(rootPath);
						var assembly = System.Reflection.Assembly.GetExecutingAssembly();


                        var resources = assembly.GetManifestResourceNames();


						using (var fileStream = new FileStream (Path.Combine (rootPath, "problem.png"), FileMode.Create))
						{
						    var stream = assembly.GetManifestResourceStream(resources.First(e => e.Contains(".problem.")));
                            Debug.Assert(stream != null, "problem.png");
						    stream.CopyTo(fileStream);
						}

						using (var fileStream = new FileStream (Path.Combine (rootPath, "warning.png"), FileMode.Create)) {
                            var stream = assembly.GetManifestResourceStream(resources.First(e => e.Contains(".warning.")));
                            Debug.Assert(stream != null, "warning.png");
						    stream.CopyTo(fileStream);
						}

						using (var fileStream = new FileStream (Path.Combine (rootPath, "fatal.png"), FileMode.Create)) {
                            var stream = assembly.GetManifestResourceStream(resources.First(e => e.Contains(".fatal.")));
                            Debug.Assert(stream != null, "fatal.png");
						    stream.CopyTo(fileStream);
						}

						using (var fileStream = new FileStream (Path.Combine (rootPath, "info.png"), FileMode.Create)) {
                            var stream = assembly.GetManifestResourceStream(resources.First(e => e.Contains(".info.")));
                            Debug.Assert(stream != null, "info.png");
						    stream.CopyTo(fileStream);
						}

						using (var fileStream = new FileStream (Path.Combine (rootPath, "sql.png"), FileMode.Create)) {
                            var stream = assembly.GetManifestResourceStream(resources.First(e => e.Contains(".sql.")));
                            Debug.Assert(stream != null, "sql.png");
						    stream.CopyTo(fileStream);
						}

						using (var fileStream = new FileStream (Path.Combine (rootPath, "database.png"), FileMode.Create)) {
                            var stream = assembly.GetManifestResourceStream(resources.First(e => e.Contains(".database.")));
                            Debug.Assert(stream != null, "database.png not found");
						    stream.CopyTo(fileStream);
						}

						using (var fileStream = new FileStream (Path.Combine (rootPath, "bug.png"), FileMode.Create)) {
                            var stream = assembly.GetManifestResourceStream(resources.First(e => e.Contains(".bug.")));
						    Debug.Assert(stream != null, "bug.png not found");
						    stream.CopyTo(fileStream);
						}

						using (var fileStream = new FileStream (Path.Combine (rootPath, "LogsTemplate.xslt"), FileMode.Create)) {
                            var stream = assembly.GetManifestResourceStream(resources.First(e => e.Contains(".LogsTemplate.")));
                            Debug.Assert(stream != null, "LogsTemplate.xslt not found");
						    stream.CopyTo(fileStream);
						}

                        Task.Factory.StartNew(CleanDiskHistory);

                    }
                    else
                        sessionNumber = Directory.GetFiles(rootPath, "*.xml").Length + 1;

                    CurrentFilePath = Path.Combine(rootPath, String.Format("Working_session_{0}.xml", sessionNumber));

                    var xmlHeader = "<?xml version=\"1.0\"?>";
                    if (Equals(_encoding, Encoding.UTF8))
                        xmlHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";

                    File.WriteAllText(CurrentFilePath, string.Format("{2}<?xml-stylesheet type='text/xsl' href='LogsTemplate.xslt'?><Logs application_name=\"{0}\" filename=\"Working_session_{1}.xml\"></Logs>", _applicationName, sessionNumber,xmlHeader), _encoding);
                }

                
            }

            public void Write(string message)
            {
                _closeTimer.Change(CloseDelay, Timeout.Infinite);

                lock (_fileStreamLocker)
                {
                    if (_fileStream == null)
                    {
                        _fileStream = File.Open(CurrentFilePath, FileMode.Open, FileAccess.Write);
                        _fileStream.Position = _fileStream.Length - _closeXmlNodeBytes.Length;
                    }

                    _fileStream.Write(_encoding.GetBytes(message), 0, message.Length);
                }
            }

            public void Close()
            {
                _closeTimer.Change(Timeout.Infinite, Timeout.Infinite);

                OnClose(null);
            }

            private void OnClose(object param)
            {
                lock (_fileStreamLocker)
                {
                    if (_fileStream != null)
                    {
                        _fileStream.Write(_closeXmlNodeBytes, 0, _closeXmlNodeBytes.Length);
                        _fileStream.Close();
                        _fileStream.Dispose();
                        _fileStream = null;
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}
