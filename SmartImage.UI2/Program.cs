// ReSharper disable RedundantUsingDirective.Global
#region Global usings

global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using NNW = System.Diagnostics.CodeAnalysis.NotNullWhenAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
global using NN2 = JetBrains.Annotations.NotNullAttribute;
global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
global using MNN = System.Diagnostics.CodeAnalysis.MemberNotNullAttribute;
global using CMN = System.Runtime.CompilerServices.CallerMemberNameAttribute;
global using JPN = System.Text.Json.Serialization.JsonPropertyNameAttribute;
global using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;
global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
global using Url = Flurl.Url;
global using USI = JetBrains.Annotations.UsedImplicitlyAttribute;
global using CAE = System.Runtime.CompilerServices.CallerArgumentExpressionAttribute;
global using ISImage = SixLabors.ImageSharp.Image;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using INN = JetBrains.Annotations.ItemNotNullAttribute;
global using MURV = JetBrains.Annotations.MustUseReturnValueAttribute;
global using R1 = SmartImage.Lib.Resources;
global using CA = JetBrains.Annotations.ContractAnnotationAttribute;

#endregion

using Avalonia;
using System;
using ReactiveUI.Avalonia;

namespace SmartImage.UI2;

internal class Program
{

	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args) => BuildAvaloniaApp()
		.StartWithClassicDesktopLifetime(args);

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.UseReactiveUI(builder =>
			{
				builder.BuildApp();
			})
			.LogToTrace();

}