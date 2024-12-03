// Author: Deci | Project: SmartImage.Rdx | Name: ServerCommand.cs
// Date: 2024/11/22 @ 03:11:26

using SmartImage.Lib;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Cli;
#nullable disable
namespace SmartImage.Rdx.Commands;

public sealed class ServerCommand : AsyncCommand<ServerCommandSettings>, IDisposable
{

	public SearchServer Server { get; }

	public SearchClient Client { get; }

	private ServerCommandSettings m_scs;

	public ServerCommand()
	{
		Client = new SearchClient(SearchConfig.Default);
		Server = new SearchServer(Client, 60900);
		m_scs = null;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, ServerCommandSettings settings)
	{
		m_scs = settings;
		
		AnsiConsole.WriteLine("Starting server");

		await Server.StartAsync().ConfigureAwait(false);

		return ConsoleFormat.EC_OK;
	}

	public void Dispose()
	{
		Server.Dispose();
	}

}