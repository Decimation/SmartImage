// Author: Deci | Project: SmartImage.Lib | Name: FlareSolverrClient.cs
// Date: 2024/10/25 @ 12:10:45

using System.Diagnostics;
using System.Reflection;
using CliWrap;
using FlareSolverrSharp;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Clients;

public sealed class FlareSolverrClient : IDisposable, ISearchConfigReceiver
{

	[MNNW(true, nameof(Client))]
	public bool HasClient => Client != null;

	[MNNW(true, nameof(Clearance))]
	public bool HasClearance => Clearance != null;

	[MNNW(true, nameof(Clearance), nameof(Client))]
	public bool IsInitialized => HasClearance && HasClient;

	public ClearanceHandler Clearance { get; private set; }

	public HttpClient Client { get; private set; }

	public bool Configure(string api)
	{
		Dispose();

		Clearance = new ClearanceHandler(api)
		{
			EnsureResponseIntegrity = false
		};

		Client = new HttpClient(Clearance);

		Trace.WriteLine($"{nameof(FlareSolverrClient)}: init {api}");

		return HasClient;
	}

	private FlareSolverrClient() { }

	static FlareSolverrClient() { }

	public static FlareSolverrClient Value { get; private set; } = new();

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(FlareSolverrClient)}");
		Clearance?.Dispose();
		Client?.Dispose();
		Clearance = null;
		Client    = null;
	}

	#region Implementation of ISearchConfigReceiver

	public ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		var ok = false;

		if (cfg.FlareSolverr) {
			ok = Configure(cfg.FlareSolverrApiUrl);
		}

		return ValueTask.FromResult(ok);
	}

	#endregion

}