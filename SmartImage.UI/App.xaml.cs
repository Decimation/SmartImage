using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using Novus.Win32;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	/// <summary>
	/// This identifier must be unique for each application.
	/// </summary>
	private const string SingleGuid = "{910e8c27-ab31-4043-9c5d-1382707e6c93}";

	private const string IPC_PIPE_NAME = "SIPC";

	private static Mutex SingleMutex;

	public NamedPipeServerStream _ps;

	private void Application_Startup(object sender, StartupEventArgs e)
	{
		SingleMutex = new Mutex(true, SingleGuid);
		var isOnlyInstance = SingleMutex.WaitOne(TimeSpan.Zero, true);

		var multipleInstances = false;
		pipe = true;

		if (multipleInstances || isOnlyInstance) {
			// Show main window
			StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);

			// Release SingleInstance Mutex
			SingleMutex.ReleaseMutex();

			StartServer();

		}
		else {
			// Bring the already running application into the foreground
			// Native.PostMessage(0xffff, AppUtil.m_registerWindowMessage, 0, 0);
			SendMessage(e);
			Shutdown();

		}
	}

	private static bool pipe;

	protected override void OnExit(ExitEventArgs e)
	{
		base.OnExit(e);

		// Stop the server before exiting the application
		pipe = false;
	}

	private static void SendMessage(StartupEventArgs e)
	{

		using (var pipe = new NamedPipeClientStream(".", IPC_PIPE_NAME, PipeDirection.Out))
		using (var stream = new StreamWriter(pipe))
		{
			pipe.Connect();

			foreach (var s in e.Args) {
				stream.WriteLine(s);
			}
		}
	}

	public delegate void MessageReceivedCallback(string s);

	public event MessageReceivedCallback PipeReceived;

	private void StartServer()
	{
		_ps = new NamedPipeServerStream(IPC_PIPE_NAME, PipeDirection.In);

		var t = new Thread(() =>
		{
			while (true) {
				_ps.WaitForConnection();
				var sr = new StreamReader(_ps);

				while (!sr.EndOfStream) {
					var v = sr.ReadLine();
					PipeReceived?.Invoke(v);
				}
				_ps.Disconnect();
			}
		})
		{
			IsBackground = true
		};
		t.Start();
	}
}