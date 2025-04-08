// Author: Deci | Project: SmartImage.Lib | Name: IEndpointEngine.cs
// Date: 2025/03/27 @ 12:03:18

using Flurl.Http;

namespace SmartImage.Lib.Engines;

#pragma warning disable CS0649

public interface IEndpointEngine
{

	public Url EndpointUrl { get; }

	// public IFlurlClient Client { get; }

}