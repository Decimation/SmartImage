// Author: Deci | Project: SmartImage.Lib | Name: FlareSolverrClient.cs
// Date: 2024/10/25 @ 12:10:45

using System.Diagnostics;
using System.Reflection;
using CliWrap;
using FlareSolverrSharp;
using FlareSolverrSharp.Solvers;
using FlareSolverrSharp.Types;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;

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

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger("FlareSolverr");

	public bool Configure(string api)
	{
		Dispose();

		Clearance = new ClearanceHandler(api)
		{
			EnsureResponseIntegrity = false
		};

		Client = new HttpClient(Clearance);

		s_logger.LogTrace("Init with {Api}", api);

		return IsInitialized;
	}

	public FlareSolverrClient([CBN] string api = FLARE_SOLVERR_API_URL_DEFAULT)
	{
		Configure(api);
	}

	static FlareSolverrClient() { }

	public async ValueTask<FlareSolverrIndexResponse> GetIndexAsync()
	{
		return (await Clearance.Solverr.GetIndexAsync());
	}

	public void Dispose()
	{
		Clearance?.Dispose();
		Client?.Dispose();
		Clearance = null;
		Client    = null;
	}

	public async ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		var ok = false;
		FlareSolverrIndexResponse idx = null;

		if (!cfg.FlareSolverr) {
			return false;
		}

		ok  = Configure(cfg.FlareSolverrApiUrl);
		idx = await GetIndexAsync();

		return ok && idx != null;
	}


	public const string FLARE_SOLVERR_API_URL_DEFAULT = "http://localhost:8191";

}