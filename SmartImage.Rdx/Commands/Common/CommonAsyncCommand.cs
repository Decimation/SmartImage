// Author: Deci | Project: SmartImage.Rdx | Name: BaseAsyncCommand.cs
// Date: 2025/09/24 @ 02:09:59

#nullable disable
using SmartImage.Lib;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Commands.Common;

public abstract partial class CommonAsyncCommand<TCommandSettings> : AsyncCommand<TCommandSettings>, IDisposable
	where TCommandSettings : CommonCommandSettings
{

	public SearchConfig Config { get; protected set; }

	protected TCommandSettings CommandSettings { get; private set; }

	/// <inheritdoc />
	public abstract void Dispose();

	protected virtual void InitConfig(TCommandSettings scs)
	{
		CommandSettings = scs;

		Config.SearchEngines   = CommandSettings.SearchEngines;
		Config.PriorityEngines = CommandSettings.PriorityEngines;
		Config.UploadEngine = CommandSettings.UploadEngine;

		Config.ReadCookies = CommandSettings.ReadCookies;

		Config.FlareSolverr       = CommandSettings.FlareSolverr;
		Config.FlareSolverrApiUrl = CommandSettings.FlareSolverrApiUrl;

	}
}