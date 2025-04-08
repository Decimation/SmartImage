
using SmartImage.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Clients.Booru;

// TODO
[Experimental(AppSupport.DIAG_SMRTIMG_EXP001)]
public abstract class BaseBooruClient : IDisposable
{

	public Url BaseUrl { get; }

	public abstract string Name { get; }

	protected BaseBooruClient(Url baseUrl)
	{
		BaseUrl = baseUrl;
	}

	public virtual void Dispose() { }

}