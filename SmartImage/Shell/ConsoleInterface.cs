using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

#nullable enable
namespace SmartImage.Shell
{
	public class ConsoleInterface
	{
		public ConsoleOption[] Options { get; }

		public bool SelectMultiple { get; }

		public string? Name { get; }

		public ConsoleOption this[int i]
		{
			get
			{
				var o = Options[i];

				ConsoleOption.CheckOption(ref o);

				return o;
			}
		}

		public ConsoleInterface(ConsoleOption[] options, string? name = null, bool selectMultiple = false)
		{
			Options = options;
			SelectMultiple = selectMultiple;
			Name = name;
		}
	}
}