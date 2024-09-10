using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using ReactiveUI;
using Terminal.Gui;

namespace SmartImage.UI2
{
	public static class Program
	{

		public class TerminalScheduler : LocalScheduler
		{

			public static readonly TerminalScheduler Default = new();

			private TerminalScheduler() { }

			public override IDisposable Schedule<TState>(
				TState state,
				TimeSpan dueTime,
				Func<IScheduler, TState, IDisposable> action
			)
			{
				IDisposable PostOnMainLoop()
				{
					var composite    = new CompositeDisposable(2);
					var cancellation = new CancellationDisposable();

					Application.MainLoop.Invoke(
						() =>
						{
							if (!cancellation.Token.IsCancellationRequested) {
								composite.Add(action(this, state));
							}
						}
					);
					composite.Add(cancellation);

					return composite;
				}

				IDisposable PostOnMainLoopAsTimeout()
				{
					var composite = new CompositeDisposable(2);

					object timeout = Application.MainLoop.AddTimeout(
						dueTime,
						() =>
						{
							composite.Add(action(this, state));

							return false;
						}
					);
					composite.Add(Disposable.Create(() => Application.MainLoop.RemoveTimeout(timeout)));

					return composite;
				}

				return dueTime == TimeSpan.Zero
					       ? PostOnMainLoop()
					       : PostOnMainLoopAsTimeout();
			}

		}

		public class Item1
		{

			

		}
		public static void Main(string[] args)
		{
			Application.Init();
			RxApp.MainThreadScheduler = Program.TerminalScheduler.Default;
			RxApp.TaskpoolScheduler   = TaskPoolScheduler.Default;

			var tx = new TextField();

			var tv = new TreeView<Item1>();

			var w = new Window();
			w.Add(tv);

			Application.Run(w);
			Application.Top.Dispose();
			Application.Shutdown();

		}

	}
}