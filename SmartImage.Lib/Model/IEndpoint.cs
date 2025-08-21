// Author: Deci | Project: SmartImage.Lib | Name: IEndpointEngine.cs
// Date: 2025/03/27 @ 12:03:18

namespace SmartImage.Lib.Model;

#pragma warning disable CS0649

public interface IEndpoint
{

	public Url Endpoint { get; }

	// public IFlurlClient Client { get; }

}