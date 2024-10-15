// Author: Deci | Project: SmartImage.Lib | Name: CookiesManager.cs

using System.Data;
using System.Diagnostics;
using System.Net;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using Flurl.Http;
using Kantan.Net.Web;
using SmartImage.Lib.Results.Data;

namespace SmartImage.Lib.Utilities;

public class AsyncLazy<T> : Lazy<Task<T>>
{

	public AsyncLazy(Func<T> valueFactory, CancellationToken ct = default) :
		base(() => Task.Factory.StartNew(valueFactory, ct)) { }

	public AsyncLazy(Func<Task<T>> taskFactory, CancellationToken ct = default) :
		base(() => Task.Factory.StartNew(() => taskFactory(), ct).Unwrap()) { }

	public AsyncLazy(Func<object, T> valueFactory, CancellationToken ct = default)
		: base(() => Task.Factory.StartNew((z) => valueFactory(z), ct)) { }

	public AsyncLazy(Func<object, Task<T>> valueFactory, CancellationToken ct = default)
		: base(() => Task.Factory.StartNew((z) => valueFactory(z), ct).Unwrap()) { }

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


	public IEnumerable<IBrowserCookie> Cookies { get; }

	public FirefoxCookiesProvider()
	{
		m_reader = new FirefoxCookieReader();
		Cookies = [];
	}

	public async ValueTask<bool> LoadCookiesAsync(CancellationToken ct = default)
	{
		if (((ICookieProvider) this).Loaded) {
			if (m_reader.Connection.State == ConnectionState.Open) {
				await m_reader.OpenAsync();

			}

			Cookies = await m_reader.ReadCookiesAsync();

		}


		return ((ICookieProvider) this).Loaded;

	}

	public void Dispose()
	{
		m_reader.Dispose();
	}

}

public class CookiesManager : IDisposable
{

	// TODO

	// [DebuggerHidden]
	private static async Task<List<IBrowserCookie>> ReadCookiesAsync() { }

	public List<IBrowserCookie> Cookies { get; private set; }

	[MNNW(true, nameof(Cookies))]
	public bool Loaded => Cookies != null;

	private CookiesManager()
	{
		Cookies = null;
	}

	public async Task<bool> LoadCookiesAsync(bool force = false)
	{
		var b = !Loaded || force;

		if (b) {
			Cookies = await ReadCookiesAsync();

		}

		return Loaded;
	}

	public static readonly CookiesManager Instance = new();

	public void Dispose()
	{
		Cookies.Clear();
		Cookies = null;
	}

}