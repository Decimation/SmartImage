using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class BingEngine : BaseSearchEngine
{
    public BingEngine() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }

    public override SearchEngineOptions EngineOption => SearchEngineOptions.Bing;

    public override void Dispose() { }

    // Parsing does not seem feasible ATM
}