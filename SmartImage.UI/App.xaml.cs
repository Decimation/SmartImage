﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using Microsoft.VisualBasic.Logging;
using System.Windows.Interop;
using JetBrains.Annotations;
using Novus.Utilities;
using Novus.Win32;
#nullable disable
namespace SmartImage.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

	/// <summary>
	/// This identifier must be unique for each application.
	/// </summary>
	public const string SINGLE_GUID = "{910e8c27-ab31-4043-9c5d-1382707e6c93}";

	public const string IPC_PIPE_NAME = "SIPC";

	public const char ARGS_DELIM = '\0';

	private static Mutex _singleMutex;

	public NamedPipeServerStream PipeServer { get; private set; }

	public Thread PipeThread { get; private set; }

	private void Application_Startup(object sender, StartupEventArgs startupArgs)
	{
		bool multipleInstances = false, pipeServer = true;

		var       enumerator = startupArgs.Args.GetEnumerator();
		using var unknown    = enumerator as IDisposable;

		while (enumerator.MoveNext()) {
			var el  = enumerator.Current;
			var els = el?.ToString();

			switch (els) {
				case "-mi":
					multipleInstances = true;
					break;
				case "-nms":
					pipeServer = false;
					break;
				default:
					break;
			}
		}

		_singleMutex = new Mutex(true, SINGLE_GUID);
		var isOnlyInstance = _singleMutex.WaitOne(TimeSpan.Zero, true);

		if (multipleInstances || isOnlyInstance) {
			// Show main window
			StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);

			// Release SingleInstance Mutex
			_singleMutex.ReleaseMutex();
			if (pipeServer) {
				StartServer();

			}
		}
		else {
			// Bring the already running application into the foreground
			// Native.PostMessage(0xffff, AppUtil.m_registerWindowMessage, 0, 0);
			SendMessage(startupArgs);

			Shutdown();

		}

	}

	private static void SendMessage(StartupEventArgs e)
	{

		using var pipe = new NamedPipeClientStream(".", IPC_PIPE_NAME, PipeDirection.Out);

		using var stream = new StreamWriter(pipe);

		pipe.Connect();

		foreach (var s in e.Args) {
			stream.WriteLine(s);
		}

		stream.Write($"{ARGS_DELIM}{ProcessHelper.GetParent().Id}");

	}

	public delegate void PipeMessageCallback(string s);

	public event PipeMessageCallback OnPipeMessage;

	[DebuggerHidden]
	private void StartServer()
	{
		PipeServer = new NamedPipeServerStream(IPC_PIPE_NAME, PipeDirection.In);

		PipeThread = new Thread([DebuggerHidden]() =>
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