using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace SmartImage.Shell
{
	public class ConsoleInterface
	{
		public static readonly string DefaultName = RuntimeInfo.NAME_BANNER;

		public ConsoleInterface(IEnumerable<ConsoleOption> options, string? name = null, bool selectMultiple = false)
		{
			Options = options;
			SelectMultiple = selectMultiple;
			Name = name ?? DefaultName;
		}

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
	}
}