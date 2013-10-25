using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTraceLib;
using NUnit.Framework;
using AdvancedTraceListeners.Xml;
using System.Xml;

namespace AdvancedTraceListenersTest.Xml
{
	[TestFixture]
	public class XmlWriterTraceListenerTest
	{
		[Test]
		[ExpectedException ("System.ArgumentException")]
		public void InstanciationExceptionWithHistoryDayEqualTo0 ()
		{
			using (var log = new XmlWriterTraceListener("Application 1",null,0)) {
			}
		}

		[Test]
		[ExpectedException ("System.ArgumentException")]
		public void InstanciationExceptionWithHistoryDayNegative ()
		{
			using (var log = new XmlWriterTraceListener("Application 1", null, -1)) {
			}
		}

		[Test]
		[ExpectedException ("System.IO.DirectoryNotFoundException")]
		public void InstanciationExceptionWithCustomPathInvalid ()
		{
			using (var logStorage = new XmlWriterTraceListener("Application 1", @"B:\")) {
				logStorage.WriteLineEx ("Test", "1");
			}
		}

		[Test]
		public void InstanciationVerifyCreationDirectory ()
		{
			var path = AppDomain.CurrentDomain.BaseDirectory;
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd")), true);

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory)) {
				logStorage.WriteLineEx ("Test", "1");
				logStorage.Flush ();

				var nbDirectoryWithCurrentDay = Directory.GetDirectories (logStorage.BaseRootPath).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd"));
				Assert.IsTrue (nbDirectoryWithCurrentDay == 1, "Nb Directory With Current Day Name : " + nbDirectoryWithCurrentDay);
			}
           
		}

		[Test]
		public void InstanciationVerifyCreationFile ()
		{
			InstanciationVerifyCreationDirectory ();

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			var pathFileSession = Path.Combine (pathDirectoryDaily, "Working_session_1.xml");

			Assert.IsTrue (File.Exists (pathFileSession));
		}

		[Test]
		public void Instanciation5FileCreateFor5Instanciation ()
		{
			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (pathDirectoryDaily, true);

			for (int i=1; i<=5; i++) {
                
				var pathFileSession = Path.Combine (pathDirectoryDaily, "Working_session_" + i + ".xml");

				using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory)) {
					logStorage.WriteLineEx ("Test", "1");
				}

				Assert.IsTrue (File.Exists (pathFileSession));
			}
		}

		[Test]
		public void InstanciationCheckExistingFilesInDirectory ()
		{
			InstanciationVerifyCreationDirectory ();
			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));

			var filesInDirectory = Directory.GetFiles (pathDirectoryDaily).Select (Path.GetFileName).ToList ();

			Assert.IsTrue (filesInDirectory.Count == 9, filesInDirectory.Count + " files found in directory");
			Assert.IsTrue (filesInDirectory.Contains ("Working_session_1.xml"), "Working_session_1.xml is missing");
			Assert.IsTrue (filesInDirectory.Contains ("LogsTemplate.xslt"), "LogsTemplate.xslt is missing");
			Assert.IsTrue (filesInDirectory.Contains ("problem.png"), "problem.png is missing");
			Assert.IsTrue (filesInDirectory.Contains ("warning.png"), "warning.png is missing");
			Assert.IsTrue (filesInDirectory.Contains ("fatal.png"), "fatal.png is missing");
			Assert.IsTrue (filesInDirectory.Contains ("info.png"), "info.png is missing");
			Assert.IsTrue (filesInDirectory.Contains ("bug.png"), "bug.png is missing");
			Assert.IsTrue (filesInDirectory.Contains ("database.png"), "database.png is missing");
			Assert.IsTrue (filesInDirectory.Contains ("sql.png"), "sql.png is missing");
		}

		[Test]
		public void CheckHtmlMessageContent ()
		{
			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (pathDirectoryDaily, true);

			var message = "<html><head></head><body><form name=\"form1\" method=\"POST\" action=\"https://secure.ogone.com/ncol/test/orderstandard_UTF8.asp\" ><table><tr><td>PSPID</td><td><input name=\"PSPID\" type=\"Text\" value=\"kmedia\" style=\"width:600px;\"></td></tr><tr><td>ORDERID</td><td><input name=\"ORDERID\" type=\"Text\" value=\"39\" style=\"width:600px;\"></td></tr><tr><td>AMOUNT</td><td><input name=\"AMOUNT\" type=\"Text\" value=\"41860\" style=\"width:600px;\"></td></tr><tr><td>CURRENCY</td><td><input name=\"CURRENCY\" type=\"Text\" value=\"EUR\" style=\"width:600px;\"></td></tr><tr><td>LANGUAGE</td><td><input name=\"LANGUAGE\" type=\"Text\" value=\"fr_FR\" style=\"width:600px;\"></td></tr><tr><td>EMAIL</td><td><input name=\"EMAIL\" type=\"Text\" value=\"\" style=\"width:600px;\"></td></tr><tr><td>ECOM_BILLTO_POSTAL_NAME_FIRST</td><td><input name=\"ECOM_BILLTO_POSTAL_NAME_FIRST\" type=\"Text\" value=\"\" style=\"width:600px;\"></td></tr><tr><td>ECOM_BILLTO_POSTAL_NAME_LAST</td><td><input name=\"ECOM_BILLTO_POSTAL_NAME_LAST\" type=\"Text\" value=\"\" style=\"width:600px;\"></td></tr><tr><td>CN</td><td><input name=\"CN\" type=\"Text\" value=\"test\" style=\"width:600px;\"></td></tr><tr><td>PM</td><td><input name=\"PM\" type=\"Text\" value=\"CreditCard\" style=\"width:600px;\"></td></tr><tr><td>TITLE</td><td><input name=\"TITLE\" type=\"Text\" value=\"Album de l'ann?e\" style=\"width:600px;\"></td></tr><tr><td>BGCOLOR</td><td><input name=\"BGCOLOR\" type=\"Text\" value=\"#4e84c4\" style=\"width:600px;\"></td></tr><tr><td>TXTCOLOR</td><td><input name=\"TXTCOLOR\" type=\"Text\" value=\"#FFFFFF\" style=\"width:600px;\"></td></tr><tr><td>TBLBGCOLOR</td><td><input name=\"TBLBGCOLOR\" type=\"Text\" value=\"#FFFFFF\" style=\"width:600px;\"></td></tr><tr><td>TBLTXTCOLOR</td><td><input name=\"TBLTXTCOLOR\" type=\"Text\" value=\"#000000\" style=\"width:600px;\"></td></tr><tr><td>BUTTONBGCOLOR</td><td><input name=\"BUTTONBGCOLOR\" type=\"Text\" value=\"#00467F\" style=\"width:600px;\"></td></tr><tr><td>BUTTONTXTCOLOR</td><td><input name=\"BUTTONTXTCOLOR\" type=\"Text\" value=\"#FFFFFF\" style=\"width:600px;\"></td></tr><tr><td>FONTTYPE</td><td><input name=\"FONTTYPE\" type=\"Text\" value=\"Verdana\" style=\"width:600px;\"></td></tr><tr><td>accepturl</td><td><input name=\"accepturl\" type=\"Text\" value=\"http://193.178.140.140/fr-FR/Payment/PaySuccess\" style=\"width:600px;\"></td></tr><tr><td>exceptionurl</td><td><input name=\"exceptionurl\" type=\"Text\" value=\"http://193.178.140.140/fr-FR/Payment/PayException\" style=\"width:600px;\"></td></tr><tr><td>cancelurl</td><td><input name=\"cancelurl\" type=\"Text\" value=\"http://193.178.140.140/fr-FR/Payment/PayCancel\" style=\"width:600px;\"></td></tr><tr><td>BACKURL</td><td><input name=\"BACKURL\" type=\"Text\" value=\"http://193.178.140.140/fr-FR/Indexing/Payment\" style=\"width:600px;\"></td></tr><tr><td>HOMEURL</td><td><input name=\"HOMEURL\" type=\"Text\" value=\"http://193.178.140.140/fr-FR\" style=\"width:600px;\"></td></tr><tr><td>DECLINEURL</td><td><input name=\"DECLINEURL\" type=\"Text\" value=\"http://193.178.140.140/fr-FR/Payment/PayDecline\" style=\"width:600px;\"></td></tr><tr><td>SHASign</td><td><input name=\"SHASign\" type=\"Text\" value=\"0F980110CB081C6AB8FCD1359553D2BA3CBAC87C\" style=\"width:600px;\"></td></tr><tr><td colspan=\"2\"><input type=\"submit\" value=\"Submit\"/></td></tr></table></form><input name=\"Concat string\" type=\"Text\" value=\"ACCEPTURL=http://193.178.140.140/fr-FR/Payment/PaySuccessalexandresebastien12AMOUNT=41860alexandresebastien12BACKURL=http://193.178.140.140/fr-FR/Indexing/Paymentalexandresebastien12BGCOLOR=#4e84c4alexandresebastien12BUTTONBGCOLOR=#00467Falexandresebastien12BUTTONTXTCOLOR=#FFFFFFalexandresebastien12CANCELURL=http://193.178.140.140/fr-FR/Payment/PayCancelalexandresebastien12CN=testalexandresebastien12CURRENCY=EURalexandresebastien12DECLINEURL=http://193.178.140.140/fr-FR/Payment/PayDeclinealexandresebastien12EXCEPTIONURL=http://193.178.140.140/fr-FR/Payment/PayExceptionalexandresebastien12FONTTYPE=Verdanaalexandresebastien12HOMEURL=http://193.178.140.140/fr-FRalexandresebastien12LANGUAGE=fr_FRalexandresebastien12ORDERID=39alexandresebastien12PM=CreditCardalexandresebastien12PSPID=kmediaalexandresebastien12TBLBGCOLOR=#FFFFFFalexandresebastien12TBLTXTCOLOR=#000000alexandresebastien12TITLE=Album de l&#39;ann&#233;ealexandresebastien12TXTCOLOR=#FFFFFFalexandresebastien12\" style=\"width:600px;\"></body></html>";

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory,2, false)) {
				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);
				AdvancedTrace.TraceInformation (message, "Info");

				AdvancedTrace.RemoveTraceListener (AdvancedTrace.ListenerType.All, logStorage);

			}

			var xmlDoc = new XmlDocument ();
			xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
		}

		[Test]
		public void CheckXmlValidityForNewFile ()
		{
			InstanciationVerifyCreationDirectory ();
			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
            
			var xmlDoc = new XmlDocument ();
			xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
		}

		[Test]
		public void Check1TraceInformationWithoutDelayed ()
		{

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (pathDirectoryDaily, true);

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory, 2, false)) {
				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);
				AdvancedTrace.TraceInformation ("Information", "Info");

				AdvancedTrace.RemoveTraceListener (AdvancedTrace.ListenerType.All, logStorage);
			}
			var xmlDoc = new XmlDocument ();
			xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
		}

		[Test]
		public void Check1TraceErrorWithoutDelayed ()
		{

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (pathDirectoryDaily, true);

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory, 2, false)) {

				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);

				try {
					throw new Exception ("Exception 1");
				} catch (Exception e) {
					try {
						throw new Exception ("Exception 2", e);
					} catch (Exception e1) {
						AdvancedTrace.TraceError ("MyError", e1, "Info");
					}
				}

				AdvancedTrace.RemoveTraceListener (AdvancedTrace.ListenerType.All, logStorage);
			}
			var xmlDoc = new XmlDocument ();
			xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
		}

		[Test]
		public void Check1TraceWarningWithoutDelayed ()
		{

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (pathDirectoryDaily, true);

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory, 2, false)) {

				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);
				AdvancedTrace.TraceWarning ("MyWarning", "Info");

                
				AdvancedTrace.RemoveTraceListener (AdvancedTrace.ListenerType.All, logStorage);
			}
			var xmlDoc = new XmlDocument ();
			xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
		}

		[Test]
		public void Check1TraceInformation1TraceError1TraceWarningWithoutDelayed ()
		{

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (pathDirectoryDaily, true);

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory, 2, false)) {
				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);
				AdvancedTrace.TraceInformation ("MyInformation", "Info");
				AdvancedTrace.TraceWarning ("MyWarning", "Info");

				try {
					throw new Exception ("Exception 1");
				} catch (Exception e) {
					try {
						throw new Exception ("Exception 2", e);
					} catch (Exception e1) {
						try {
							throw new Exception ("Exception 3", e1);
						} catch (Exception e2) {
							try {
								throw new Exception ("Exception 4", e2);
							} catch (Exception e3) {
								try {
									throw new Exception ("Exception 5", e3);
								} catch (Exception e4) {
									AdvancedTrace.TraceError ("MyError", e4, "Info");
								}
							}
						}
					}
				}

				AdvancedTrace.RemoveTraceListener (AdvancedTrace.ListenerType.All, logStorage);

			}

			var xmlDoc = new XmlDocument ();
			xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
		}

		[Test]
		public void StressTrace10ThreadAnd10000TraceWithoutDelayed ()
		{

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory, 2, false)) {
				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);
				var tasks = new List<Task> ();

				var stopWatch = new System.Diagnostics.Stopwatch ();
				stopWatch.Start ();
				for (int i = 0; i < 10; i++) {
					int i1 = i;
					tasks.Add (Task.Factory.StartNew (() => {

						for (int j = 0; j < 10000; j++) {
							AdvancedTrace.TraceInformation ("MyInformation " + i1 + " " + j, "Info");
						}

					}));
				}

				Task.WaitAll (tasks.ToArray ());

				logStorage.Flush ();

				stopWatch.Stop ();
				var ts = stopWatch.Elapsed;
				System.Diagnostics.Debug.WriteLine ("Time Execute Trace :" + String.Format ("{0:00}:{1:00}:{2:00}.{3:00}",
				                                                                                      ts.Hours, ts.Minutes, ts.Seconds,
				                                                                                      ts.Milliseconds / 10));

				AdvancedTrace.RemoveTraceListener (AdvancedTrace.ListenerType.All, logStorage);

			}

			var xmlDoc = new XmlDocument ();
			xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
		}

		[Test]
		public void StressTrace1ThreadAnd100000TraceWithoutDelayed ()
		{

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory, 2, false)) {
				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);

				var stopWatch = new System.Diagnostics.Stopwatch ();
				stopWatch.Start ();

				for (int j = 0; j < 100000; j++) {
					AdvancedTrace.TraceInformation ("MyInformation " + j, "Info");
				}

				logStorage.Flush ();

				stopWatch.Stop ();
				var ts = stopWatch.Elapsed;
				System.Diagnostics.Debug.WriteLine ("Time Execute Trace :" + String.Format ("{0:00}:{1:00}:{2:00}.{3:00}",
				                                                                                      ts.Hours, ts.Minutes, ts.Seconds,
				                                                                                      ts.Milliseconds / 10));

				AdvancedTrace.RemoveTraceListener (AdvancedTrace.ListenerType.All, logStorage);
			}

			var xmlDoc = new XmlDocument ();
			xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
		}
	}

	public class XmlWriterTraceListenerWithDelayedTest
	{
		[Test]
		public void Check1TraceInformationWithDelayed ()
		{

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (pathDirectoryDaily, true);

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory)) {
				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);
				AdvancedTrace.TraceInformation ("Information", "Info");

				Thread.Sleep (31000);

				var xmlDoc = new XmlDocument ();
		        xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));

				AdvancedTrace.RemoveTraceListener (AdvancedTrace.ListenerType.All, logStorage);
			}
		}

		[Test]
		public void StressTrace10ThreadAnd10000TraceWithDelayed ()
		{

			var path = AppDomain.CurrentDomain.BaseDirectory;
			var pathDirectoryDaily = Path.Combine (path, DateTime.Now.ToString ("yyyy-MM-dd"));
			if (Directory.GetDirectories (path).Count (p => Path.GetFileName (p) == DateTime.Now.ToString ("yyyy-MM-dd")) == 1)
				Directory.Delete (pathDirectoryDaily, true);

			System.Diagnostics.Debug.WriteLine ("DirectoryCreated");

			using (var logStorage = new XmlWriterTraceListener("Application 1", AppDomain.CurrentDomain.BaseDirectory, 2, true)) {

				AdvancedTrace.AddTraceListener (AdvancedTrace.ListenerType.All, logStorage);
				var tasks = new List<Task> ();

				System.Diagnostics.Debug.WriteLine ("Listener added");

				var stopWatch = new System.Diagnostics.Stopwatch ();
				stopWatch.Start ();
				for (int i = 0; i < 10; i++) {
					int i1 = i;
					tasks.Add (Task.Factory.StartNew (() => {

						for (int j = 0; j < 10000; j++) {
							AdvancedTrace.TraceInformation ("MyInformation " + i1 + " " + j, "Info");
						}

					}));
				}

				logStorage.Flush ();


				Task.WaitAll (tasks.ToArray ());

				System.Diagnostics.Debug.WriteLine ("Tasks finished added");

				stopWatch.Stop ();
				var ts = stopWatch.Elapsed;
				System.Diagnostics.Debug.WriteLine ("Time Execute Trace :" + String.Format ("{0:00}:{1:00}:{2:00}.{3:00}",
				                                                                          ts.Hours, ts.Minutes, ts.Seconds,
				                                                                          ts.Milliseconds / 10));

				Thread.Sleep (31000);

				var xmlDoc = new XmlDocument ();
				xmlDoc.Load (Path.Combine (pathDirectoryDaily, "Working_session_1.xml"));
			}
		}
	}
}
