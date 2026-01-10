
using SmartImage.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Clients.Booru;

// TODO
[Experimental(AppSupport.DIAG_ID_EXPERIMENTAL)]
public abstract class BaseBooruClient : IDisposable, IUrl
{

	public Url Url { get; }

	public abstract string Name { get; }

	protected BaseBooruClient(Url url)
	{
		Url = url;
	}

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}

}