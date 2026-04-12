// Author: Deci | Project: SmartImage.Rdx | Name: BaseConsoleAction.cs
// Date: 2026/02/21 @ 14:02:02

#nullable disable
using SmartImage;
using Spectre.Console.Rendering;

// ReSharper disable InconsistentNaming

namespace SmartImage.Rdx.Shell;

// todo
public class BaseConsoleAction<T, T2>
{

	public ConsoleKey Key { get; init; }

	public string Description { get; init; }

	public Func<T, T2, bool> Func { get; init; }

}