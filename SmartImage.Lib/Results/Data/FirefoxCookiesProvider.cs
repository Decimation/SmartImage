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

public class FirefoxCookiesProvider : ICookieProvider
{

	private readonly FirefoxCookieReader m_reader;


	public IList<IBrowserCookie> Cookies { get; private set; }

	public FirefoxCookiesProvider()
	{
		m_reader = new FirefoxCookieReader();
		Cookies  = null;
	}

	public async ValueTask<bool> LoadCookiesAsync(CancellationToken ct = default)
	{
		if (!((ICookieProvider) this).Loaded) {
			if (m_reader.Connection.State != ConnectionState.Open) {
				await m_reader.OpenAsync();

			}

			Cookies = await m_reader.ReadCookiesAsync();

		}


		return ((ICookieProvider) this).Loaded;

	}

	public void Dispose()
	{
		Cookies.Clear();
		Cookies = null;
		m_reader.Dispose();
	}

	public static readonly ICookieProvider Instance = new FirefoxCookiesProvider();

}