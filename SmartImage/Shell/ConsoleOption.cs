using System;

#nullable enable
namespace SmartImage.Shell
{
	/// <summary>
	/// Represents an interactive console/shell option
	/// </summary>
	public class ConsoleOption
	{
		public static readonly ConsoleColor DefaultOptionColor = Console.ForegroundColor;

		public ConsoleOption() { }


		public virtual string Name { get; internal set; }

		public virtual Func<object?> Function { get; internal set; }

		public virtual Func<object>? AltFunction { get; internal set; }

		public virtual string? Data { get; internal set; }

		public virtual ConsoleColor Color { get; internal set; } = DefaultOptionColor;

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