using System;

#nullable enable
namespace SmartImage.Shell
{
	public class ConsoleOption
	{
		public static readonly ConsoleColor DefaultOptionColor = Console.ForegroundColor;

		public ConsoleOption()
		{
			Color = DefaultOptionColor;
		}

		public ConsoleOption(string name, ConsoleColor color, Func<object> func)
			: this(name, func, null, null, color) { }

		public ConsoleOption(string name, Func<object> func) : this(name, DefaultOptionColor, func) { }

		public ConsoleOption(string name, Func<object> func, Func<object>? altFunc, string? data, ConsoleColor color)
		{
			Name = name;
			Function = func;
			AltFunction = altFunc;
			Data = data;
			Color = color;
		}

		public virtual string Name { get; }

		public virtual Func<object> Function { get; }

		public virtual Func<object>? AltFunction { get; internal set; }

		public virtual string? Data { get; }

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