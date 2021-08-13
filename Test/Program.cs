using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl;
using SmartImage.Lib.Engines.Impl.Other;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

#pragma warning disable IDE0079
#pragma warning disable CS0168, CS1998
#pragma warning disable IDE0060

namespace Test
{
	/* 
	 * >>> SmartImage.Lib <<<
	 *
	 *
	 * - SearchClient is used to run advanced searches and utilizes multiple engines
	 * - Individual engines can also be used
	 *
	 */


	public static class Program
	{
		public static void OnResult(object _, ResultCompletedEventArgs e)
		{

			if (e.Result.IsSuccessful) {
				Console.WriteLine(e.Result);
			}
		}

		public static async Task Main(string[] args)
		{
			
			var q   = new ImageQuery(@"C:\Users\Deci\Pictures\Test Images\Test6.jpg");
			var engine  = new SauceNaoEngine() { };
			engine.Authentication = "362e7e82bc8cf7f6025431fbf3006510057298c3";
			var  task = engine.GetResultAsync(q);

			var engine2 = new IqdbEngine();
			var task2   = engine2.GetResultAsync(q);

			var tasks = new[] {task, task2};
			Task.WaitAny(tasks);

			Console.WriteLine("waiting");
			var result = await task;
			

			Console.WriteLine(">> {0}", result);
			var result2 = await task2;
			Console.WriteLine(">> {0}", result2);


		}
	}
}