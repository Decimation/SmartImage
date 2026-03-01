// Author: Deci | Project: SmartImage.Rdx | Name: ConsoleAction.cs
// Date: 2026/02/21 @ 14:02:02

#nullable disable
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx.Commands.Search;

// todo
internal class ConsoleAction
{

	public string Descr { get;  }

	public delegate bool RenderableFunction(LiveDisplayContext ldc, IRenderable ci);

	public RenderableFunction Func {get;}
}