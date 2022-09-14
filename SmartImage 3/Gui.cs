// ReSharper disable InconsistentNaming

#region Global usings

global using G = SmartImage.Gui;
global using R = SmartImage.Resources;
global using GV = SmartImage.Gui.Values;
global using GS = SmartImage.Gui.Styles;
global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;

#endregion

#nullable disable
using System.Runtime.CompilerServices;

namespace SmartImage;

/// <summary>
/// Root class for Gui
/// </summary>
public static partial class Gui
{
	public static void Init()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Values).TypeHandle);
	}
}