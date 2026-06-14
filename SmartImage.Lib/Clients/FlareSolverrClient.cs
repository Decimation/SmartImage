// Author: Deci | Project: SmartImage.Lib | Name: FlareSolverrClient.cs
// Date: 2024/10/25 @ 12:10:45

using FlareSolverrSharp;
using FlareSolverrSharp.Solvers;
using FlareSolverrSharp.Types;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Clients;

public sealed class FlareSolverrClient : IDisposable, ISearchConfigReceiver
{

	[MNNW(true, nameof(Clearance))]
	public bool HasClearance => Clearance != null;

	[MNNW(true, nameof(Clearance))]
	public bool IsInitialized => HasClearance;

	public ClearanceHandler Clearance { get; private set; }


	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger("FlareSolverr");

	public bool Configure(string api)
	{
		Clearance = new ClearanceHandler(api)
		{
			EnsureResponseIntegrity = false,
		};

		s_logger.LogTrace("Init with {Api}", api);

		return IsInitialized;
	}

	public FlareSolverrClient([CBN] string api = FLARE_SOLVERR_API_URL_DEFAULT)
	{
		Configure(api);
	}

	public FlareSolverrClient() { }

	static FlareSolverrClient() { }

	public async ValueTask<FlareSolverrIndexResponse> GetIndexAsync()
	{
		return (await Clearance.Solverr.GetIndexAsync());
	}

	public async ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		var ok = false;

		FlareSolverrIndexResponse idx = null;

		if (!cfg.FlareSolverr) {
			return false;
		}

		idx = await FlareSolverr.TryGetIndexAsync(new Uri(cfg.FlareSolverrApiUrl));

		ok = idx != null && Configure(cfg.FlareSolverrApiUrl);

		return ok;
	}


	public const string FLARE_SOLVERR_API_URL_DEFAULT = "http://localhost:8191";

	public void Dispose()
	{
		Clearance?.Dispose();
		Clearance = null;
	}

}