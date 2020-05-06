using System;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using SmartImage.Searching;

namespace SmartImage
{
	public static class CliUtilities
	{
		public static Type[] LoadVerbs()
		{
			return Assembly.GetExecutingAssembly().GetTypes()
			               .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
		}

		public static void Run(object obj)
		{
			switch (obj) {
				case CtxMenuOptions c:
					Console.WriteLine(c);
					break;
				case PathIntegration c:
					Console.WriteLine(c);
					break;
				case CreateSauceNao c:
					Console.WriteLine(c);
					break;
				case Reset c:
					Console.WriteLine(c);
					break;
				case Info c:
					Console.WriteLine(c);
					break;
			}
		}

		public static void HandleErrors(object obj)
		{
			
			Console.WriteLine("error: {0}", obj);
		}

		public class Arguments
		{
			[Option("search-engines", Separator = ',')]
			public SearchEngines Engines { get; set; }

			[Option("priority-engines", Separator = ',')]
			public SearchEngines PriorityEngines { get; set; }

			[Option("imgur-auth")]
			public string ImgurAuth { get; set; }

			[Option("saucenao-auth")]
			public string SauceNaoAuth { get; set; }
			
			[Value(0, Required = true)]
			public string Image { get; set; }


			public override string ToString()
			{
				var sb = new StringBuilder();

				sb.AppendFormat("Search engines: {0}\n", Engines);
				sb.AppendFormat("Priority engines: {0}\n", PriorityEngines);
				sb.AppendFormat("Imgur auth: {0}\n", ImgurAuth);
				sb.AppendFormat("SauceNao auth: {0}\n", SauceNaoAuth);
				sb.AppendFormat("Image: {0}\n", Image);
				
				return sb.ToString();
			}
		}

		[Verb("ctx-menu")]
		public class CtxMenuOptions
		{
			[Option]
			public bool Add { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Add: {0}\n", Add);
				return sb.ToString();
			}
		}

		[Verb("path")]
		public class PathIntegration
		{
			[Option]
			public bool Add { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Add: {0}\n", Add);
				return sb.ToString();
			}
		}

		[Verb("create-saucenao")]
		public class CreateSauceNao
		{
			[Option]
			public bool Auto { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Auto: {0}\n", Auto);
				return sb.ToString();
			}
		}

		[Verb("reset")]
		public class Reset
		{
			[Option]
			public bool All { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("All: {0}\n", All);
				return sb.ToString();
			}
		}

		[Verb("info")]
		public class Info
		{
			[Option]
			public bool Full { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Full: {0}\n", Full);
				return sb.ToString();
			}
		}
	}
}