using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib
{
	public class IqdbEngine : SearchEngine
	{
		public IqdbEngine() : base("https://iqdb.org/?url=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Iqdb;

	}
}
