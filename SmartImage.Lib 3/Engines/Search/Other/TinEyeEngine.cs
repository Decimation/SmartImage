using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class TinEyeEngine : BaseSearchEngine
{
    public TinEyeEngine() : base("https://www.tineye.com/search?url=") { }

    public override SearchEngineOptions EngineOption => SearchEngineOptions.TinEye;

    public override void Dispose()
    {
    }
}