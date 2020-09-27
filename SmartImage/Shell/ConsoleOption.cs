using System;
using System.Media;

#nullable enable
namespace SmartImage.Shell
{
	/// <summary>
	/// Represents an interactive console/shell option
	/// </summary>
	public class ConsoleOption
	{
		public static readonly ConsoleColor DefaultOptionColor = Console.ForegroundColor;

		public ConsoleOption()
		{

		}

		/// <summary>
		/// Represents a <see cref="ConsoleOption"/> which is not yet ready
		/// </summary>
		public static readonly ConsoleOption Wait = new ConsoleOption()
		{
			Name =  "Wait",

			Color = ConsoleColor.Yellow,

			Function = () =>
			{
				SystemSounds.Exclamation.Play();

				return null;
			},
			AltFunction = () =>
			{

				return null;
			}
		};


		/// <summary>
		/// Display name
		/// </summary>
		public virtual string Name { get; internal set; }

		/// <summary>
		/// Function to execute when selected
		/// </summary>
		public virtual Func<object?> Function { get; internal set; }
		
		/// <summary>
		/// Function to execute when selected with modifiers (<see cref="ConsoleIO.ALT_EXTRA"/>)
		/// </summary>
		public virtual Func<object>? AltFunction { get; internal set; }

		public virtual string? Data { get; internal set; }

		public virtual ConsoleColor Color { get; internal set; } = DefaultOptionColor;

		public static void CheckOption(ref ConsoleOption option)
		{
			option ??= ConsoleOption.Wait;
		}

		public static ConsoleOption[] CreateOptionsFromEnum<TEnum>() where TEnum : Enum
		{
			var options = (TEnum[]) Enum.GetValues(typeof(TEnum));
			var rg = new ConsoleOption[options.Length];

			for (int i = 0; i < rg.Length; i++) {
				var option = options[i];
				string? name = Enum.GetName(typeof(TEnum), option);

				rg[i] = new ConsoleOption()
				{
					Name = name,
					Function = () => { return option; }

				};
			}


			return rg;


		}
	}
}