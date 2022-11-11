using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartImage.Lib.Engines.Search;

public sealed class ImgOpsEngine : BaseSearchEngine
{
	public ImgOpsEngine() : base("https://imgops.com/") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.ImgOps;

	public override void Dispose() { }
}