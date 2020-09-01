using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

#nullable enable
namespace SmartImage.Model
{
	public class ConsoleOption
	{
		public virtual string Name { get; }

		public virtual Func<object> Function { get; }

		public virtual string? ExtendedName { get; }

		public ConsoleOption()
		{

		}
		public ConsoleOption(string displayName, Func<object> func) : this(displayName, func, null)
		{
			
		}
		public ConsoleOption(string displayName, Func<object> func, string? extendedName)
		{
			Name = displayName;
			Function = func;
			ExtendedName = extendedName;
		}
	}
}
