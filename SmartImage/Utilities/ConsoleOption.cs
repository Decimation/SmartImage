using System;

#nullable enable
namespace SmartImage.Utilities
{
	public class ConsoleOption
	{
		public virtual string Name { get; }

		public virtual Func<object> Function { get; }

		public virtual string? ExtendedName { get; }

		public virtual ConsoleColor Color { get; } 


		public static readonly ConsoleColor DefaultOptionColor = Console.ForegroundColor;

		public ConsoleOption()
		{
			Color = DefaultOptionColor;
		}

		public ConsoleOption(string displayName, ConsoleColor color, Func<object> func) 
			: this(displayName, func, null, color)
		{

		}

		public ConsoleOption(string displayName, Func<object> func) : this(displayName, DefaultOptionColor, func)
		{

		}

		public ConsoleOption(string displayName, Func<object> func, string? extendedName, ConsoleColor color)
		{
			Name = displayName;
			Function = func;
			ExtendedName = extendedName;
			Color = color;
		}

		public static ConsoleOption[] CreateOptionsFromEnum<TEnum>() where TEnum : Enum
		{
			var options = (TEnum[]) Enum.GetValues(typeof(TEnum));
			var rg = new ConsoleOption[options.Length];

			for (int i = 0; i < rg.Length; i++) {
				var option = options[i];
				var name = Enum.GetName(typeof(TEnum), option);

				rg[i] = new ConsoleOption(name, () => { return option; });
			}


			return rg;


		}
	}
}