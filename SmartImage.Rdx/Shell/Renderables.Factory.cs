// Author: Deci | Project: SmartImage.Rdx | Name: Renderables.Factory.cs
// Date: 2026/07/09 @ 02:07:08

using Kantan.Console;
using Novus.OS;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx.Shell;

internal static partial class Renderables
{

	


	public static Markup MarkupLink(string? u, string? s = null)
	{
		u = u is not null ? Markup.Escape(u) : null;
		s = s is not null ? Markup.Escape(s) : null;

		return !String.IsNullOrWhiteSpace(s) ? new Markup($"[link={u}]{s}[/]") : new Markup($"[link]{u}[/]");
	}

}