// Author: Deci | Project: SmartImage.Rdx | Name: BaseAsyncCommand.cs
// Date: 2025/09/24 @ 02:09:59

using SmartImage.Lib;
using Spectre.Console.Cli;

#nullable disable
namespace SmartImage.Rdx.Commands;

public abstract partial class CommonAsyncCommand<TCommandSettings>
	: AsyncCommand<TCommandSettings>, IDisposable
	where TCommandSettings : CommonCommandSettings
{

	public SearchConfig Config { get; protected set; }

	protected TCommandSettings m_scs;


	/// <inheritdoc />
	public abstract void Dispose();

	protected void InitConfig(TCommandSettings scs)
	{
		m_scs = scs;

		Config.SearchEngines   = m_scs.SearchEngines;
		Config.PriorityEngines = m_scs.PriorityEngines;

		Config.ReadCookies = m_scs.ReadCookies;

		Config.FlareSolverr       = m_scs.FlareSolverr;
		Config.FlareSolverrApiUrl = m_scs.FlareSolverrApiUrl;

	}
}