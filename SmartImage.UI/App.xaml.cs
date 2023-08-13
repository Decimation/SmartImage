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
	public const string SingleGuid = "{910e8c27-ab31-4043-9c5d-1382707e6c93}";

	public const string IPC_PIPE_NAME = "SIPC";

	public const char ARGS_DELIM = '\0';

	private static Mutex SingleMutex;

	public NamedPipeServerStream PipeServer { get; private set; }

	public Thread PipeThread { get; private set; }

	private void Application_Startup(object sender, StartupEventArgs e)
	{
		SingleMutex = new Mutex(true, SingleGuid);
		var isOnlyInstance = SingleMutex.WaitOne(TimeSpan.Zero, true);

		var multipleInstances = false;

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

	private static void SendMessage(StartupEventArgs e)
	{

		using (var pipe = new NamedPipeClientStream(".", IPC_PIPE_NAME, PipeDirection.Out))
		using (var stream = new StreamWriter(pipe)) {
			pipe.Connect();

			foreach (var s in e.Args) {
				stream.WriteLine(s);
			}

			stream.Write(ARGS_DELIM);
		}
	}

	public delegate void PipeMessageCallback(string s);

	public event PipeMessageCallback OnPipeMessage;

	private void StartServer()
	{
		PipeServer = new NamedPipeServerStream(IPC_PIPE_NAME, PipeDirection.In);

		PipeThread = new Thread(() =>
		{
			while (true) {
				PipeServer.WaitForConnection();
				var sr = new StreamReader(PipeServer);

				while (!sr.EndOfStream) {
					var v = sr.ReadLine();
					OnPipeMessage?.Invoke(v);
				}

				PipeServer.Disconnect();
			}
		})
		{
			IsBackground = true
		};
		PipeThread.Start();
	}
}