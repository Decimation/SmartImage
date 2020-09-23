using System;

#nullable enable
namespace SmartImage.Utilities
{
	public class ConsoleOption
	{
		public static readonly ConsoleColor DefaultOptionColor = Console.ForegroundColor;

		public ConsoleOption()
		{
			Color = DefaultOptionColor;
		}

		public ConsoleOption(string displayName, ConsoleColor color, Func<object> func)
			: this(displayName, func, null, null, color) { }

		public ConsoleOption(string displayName, Func<object> func) : this(displayName, DefaultOptionColor, func) { }

		public ConsoleOption(string displayName, Func<object> func, Func<object>? altFunc, string? extendedName, ConsoleColor color)
		{
			Name = displayName;
			Function = func;
			AltFunction = altFunc;
			ExtendedName = extendedName;
			Color = color;
		}

		public virtual string Name { get; }

		public virtual Func<object> Function { get; }

		public virtual Func<object>? AltFunction { get; internal set; }

		public virtual string? ExtendedName { get; }

		public virtual ConsoleColor Color { get; }

		public static ConsoleOption[] CreateOptionsFromEnum<TEnum>() where TEnum : Enum
		{
			var options = (TEnum[]) Enum.GetValues(typeof(TEnum));
			var rg = new ConsoleOption[options.Length];

			for (int i = 0; i < rg.Length; i++) {
				var option = options[i];
				string? name = Enum.GetName(typeof(TEnum), option);

				rg[i] = new ConsoleOption(name, () => { return option; });
			}


			return rg;


		}
	}
}