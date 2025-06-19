// Author: Deci | Project: SmartImage.Lib | Name: DefaultCookiesProvider.cs
// Date: 2025/02/04 @ 12:02:26

using System.Collections;
using Kantan.Net.Web;

namespace SmartImage.Lib.Cookies;

public class ListCookiesProvider : ICookiesProvider, IEnumerable<ICookie>
{

	private IList<ICookie> m_cookies;

	public ListCookiesProvider()
	{
		m_cookies = new List<ICookie>();
	}

	public ValueTask<IList<ICookie>> GetOrLoadCookiesAsync(CancellationToken ct = default)
	{
		return ValueTask.FromResult(m_cookies);
	}

	public void Dispose()
	{
		m_cookies.Clear();
	}

#region Implementation of IEnumerable

	public IEnumerator<ICookie> GetEnumerator()
	{
		return m_cookies.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable) m_cookies).GetEnumerator();
	}

#endregion

}