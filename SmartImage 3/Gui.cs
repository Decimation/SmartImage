// ReSharper disable InconsistentNaming
#region Global usings

global using G = SmartImage_3.Gui;
global using R = SmartImage_3.Resources;
global using GV = SmartImage_3.Gui.Values;
global using GS = SmartImage_3.Gui.Styles;

#endregion
#nullable disable
using System.Runtime.CompilerServices;

namespace SmartImage_3;

/// <summary>
/// Root class for Gui
/// </summary>
public static partial class Gui
{
	public static void Init()
	{
		RuntimeHelpers.RunClassConstructor(typeof(GV).TypeHandle);
	}
}