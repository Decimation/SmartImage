// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

#region Global usings

#endregion

#nullable disable

namespace SmartImage.UI;

/// <summary>
/// Root class for <see cref="Gui"/>
/// </summary>
public static partial class Gui
{
	public static void Init()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Values).TypeHandle);
	}
}