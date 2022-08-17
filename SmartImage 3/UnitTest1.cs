using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Kantan.Text;
using Novus.Utilities;
using NStack;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Terminal.Gui;

namespace SmartImage_3
{
	[SetUpFixture]
	public class SetupTrace
	{
		[OneTimeSetUp]
		public void StartTest()
		{
			Trace.Listeners.Add(new ConsoleTraceListener());
		}

		[OneTimeTearDown]
		public void EndTest()
		{
			Trace.Flush();
		}
	}

	[TestFixture]
	public class UnitTest1
	{
		[Test]
		[TestCase(typeof(Gui.Values))]
		public void Test1(Type t)
		{

			var f = (FieldInfo[]) ReflectionHelper.GetFieldsById(t, new Assembly[]
			{
				Assembly.GetAssembly(typeof(Application)),
				typeof(ustring).Assembly
			});

			foreach (FieldInfo info in f) {
				TestContext.WriteLine(info.Name);
			}
		}

		[Test]
		public void Test2() { }
	}
}