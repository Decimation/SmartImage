// Author: Deci | Project: SmartImage.Lib | Name: FlareSolverrClient.cs
// Date: 2024/10/25 @ 12:10:45

using CliWrap;
using FlareSolverrSharp;

namespace SmartImage.Lib.Clients;

public sealed class FlareSolverrClient : IDisposable
{

	public ClearanceHandler Clearance { get; private set; }

	[MNNW(true, nameof(Client))]
	public bool HasClient => Client != null;

	[MNNW(true, nameof(Clearance))]
	public bool HasClearance => Clearance != null;

	[MNNW(true, nameof(Clearance), nameof(Client))]
	public bool IsInitialized => HasClearance && HasClient;

	public HttpClient Client { get; private set; }

	public bool Configure(string api)
	{
		Clearance?.Dispose();
		Client?.Dispose();

		Clearance = new ClearanceHandler(api)
		{
			EnsureResponseIntegrity = false
		};
		Client = new HttpClient(Clearance);
		return HasClient;
	}

	private FlareSolverrClient() { }

	public static FlareSolverrClient Value { get; private set; } = new FlareSolverrClient();

	#region IDisposable

	public void Dispose()
	{
		Clearance?.Dispose();
		Client?.Dispose();
		Clearance = null;
		Client    = null;
	}

	#endregion

}