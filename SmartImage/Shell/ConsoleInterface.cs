using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

#nullable enable
namespace SmartImage.Shell
{
	public class ConsoleInterface
	{
		public IEnumerable<ConsoleOption> Options { get; }

		public bool SelectMultiple { get; }

		public string? Name { get; }

		public ConsoleOption this[int i]
		{
			get
			{
				var o = Options.ElementAt(i);

				ConsoleOption.EnsureOption(ref o);

				return o;
			}
		}

		public int Length => Options.Count();

		public ConsoleInterface(IEnumerable<ConsoleOption> options, string? name = null, bool selectMultiple = false)
		{
			Options = options;
			SelectMultiple = selectMultiple;
			Name = name;
		}
	}
}