using AngleSharp.Dom;
using Kantan.Collections;
using Kantan.Text;
using SmartImage.Lib.Model;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Novus.Win32;
using System.IO.Pipes;
using System.IO;
using System.Threading;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	/// <summary>
	/// This identifier must be unique for each application.
	/// </summary>
	private const string ApplicationSingleInstanceGuid = "{910e8c27-ab31-4043-9c5d-1382707e6c93}";

	private static System.Threading.Mutex _mutex;

	public NamedPipeServerStream _ps;

	private void Application_Startup(object sender, StartupEventArgs e)
	{
		// Create SingleInstance Mutex
		_mutex = new System.Threading.Mutex(true, ApplicationSingleInstanceGuid);
		var isOnlyInstance = _mutex.WaitOne(TimeSpan.Zero, true);

		// TODO: Read from settings if you want your users to be able to opt-in to multiple instance or not
		var multipleInstances = false;

		if (multipleInstances || isOnlyInstance) {
			// Show main window
			StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);

			// Release SingleInstance Mutex
			_mutex.ReleaseMutex();
			_ps = new NamedPipeServerStream("SIPC", PipeDirection.In);

			var t = new Thread(() =>
			{
				_ps.WaitForConnection();
				var sr = new StreamReader(_ps);
				while (!sr.EndOfStream) {
					Debug.WriteLine($"{sr.ReadLine()}");
				}
			}) { IsBackground = true };
			t.Start();

		}
		else {
			unsafe {
				// Bring the already running application into the foreground
				Native.PostMessage((IntPtr) 0xffff, AppUtil.m_registerWindowMessage, 0, 0);
				var pc = new NamedPipeClientStream(".", "SIPC", PipeDirection.Out);
				pc.Connect();
				var sw = new StreamWriter(pc);
				foreach (string s in e.Args) {
					sw.WriteLine(s);
				}
				sw.Flush();
				sw.Dispose();
				pc.Dispose();
				Shutdown();
			}

		}
	}
}