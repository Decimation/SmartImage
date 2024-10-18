// Author: Deci | Project: SmartImage.Lib | Name: CookiesManager.cs

using System.Data;
using System.Diagnostics;
using System.Net;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using Flurl.Http;
using Kantan.Net.Web;

namespace SmartImage.Lib.Results.Data;

public class AsyncLazy<T> : Lazy<Task<T>>
{

	public AsyncLazy(Func<T> valueFactory, CancellationToken ct = default) :
		base(() => Task.Factory.StartNew(valueFactory, ct)) { }

	public AsyncLazy(Func<Task<T>> taskFactory, CancellationToken ct = default) :
		base(() => Task.Factory.StartNew(taskFactory, ct).Unwrap()) { }

	public AsyncLazy(Func<object, T> valueFactory, CancellationToken ct = default)
		: base(() => Task.Factory.StartNew(valueFactory, ct)) { }

	public AsyncLazy(Func<object, Task<T>> valueFactory, CancellationToken ct = default)
		: base(() => Task.Factory.StartNew(valueFactory, ct).Unwrap()) { }

	/*public AsyncLazy(Func<object, Task<T>> valueFactory, CancellationToken ct = default)
		: base(() => Task.Factory.StartNew(z => valueFactory(z)), ct) { }*/

	public TaskAwaiter<T> GetAwaiter()
	{
		return Value.GetAwaiter();
	}

}

public class SmartCookiesProvider : ICookieProvider
{


	public CookieJar Jar { get; private set; }


	public SmartCookiesProvider()
	{
		Jar = new CookieJar();
	}

	public static BaseCookieReader GetReaderForBrowser(Browser b)
	{
		switch (b) {

			case Browser.Chromium:
			case Browser.Safari:
			case Browser.Unknown:
				goto case default;

			case Browser.Firefox:
				return new FirefoxCookieReader();

				break;


			default:
				throw new NotImplementedException(nameof(b));
		}
	}

	public async ValueTask<bool> LoadCookiesAsync(Browser b, CancellationToken ct = default)
	{
		using var reader = GetReaderForBrowser(b);
		var       ck     = await reader.ReadCookiesAsync();

		foreach (IBrowserCookie cookie in ck) {
			Jar.AddOrReplace(cookie.AsFlurlCookie());
		}

		return true;
	}


	public void Dispose()
	{
		Jar.Clear();
		Jar = null;
	}

	public static readonly ICookieProvider Instance = new SmartCookiesProvider();

}