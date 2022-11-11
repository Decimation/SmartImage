using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class GoogleImagesEngine : BaseSearchEngine
{
    public GoogleImagesEngine() : base("http://images.google.com/searchbyimage?image_url=") { }

    public override string Name => "Google Images";

    public override SearchEngineOptions EngineOption => SearchEngineOptions.GoogleImages;

    #region Overrides of BaseSearchEngine

    public override void Dispose() { }

    #endregion

    // https://html-agility-pack.net/knowledge-base/2113924/how-can-i-use-html-agility-pack-to-retrieve-all-the-images-from-a-website-
}