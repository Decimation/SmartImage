﻿global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
using System.Diagnostics;
using Flurl.Http;
using SmartImage_3.Lib.Engines;

namespace SmartImage_3.Lib;

public class SearchQuery : IDisposable
{
	public string Value { get; }

	public Stream Stream { get; }

	[MN]
	public Url Upload { get; private set; }

	public bool IsUrl { get; private set; }

	public bool IsFile { get; private set; }

	private SearchQuery([NN] string value, Stream stream)
	{
		Value  = value;
		Stream = stream;
	}

	public static readonly SearchQuery Null = new(null, Stream.Null);

	public static async Task<SearchQuery> TryCreateAsync(string value)
	{
		bool   isFile = false, isUrl = false;
		Stream stream;

		try {
			isFile = File.Exists(value);

			if (isFile) {
				stream = File.OpenRead(value);
			}
			else {
				stream = await value.GetStreamAsync();
				isUrl  = true;
			}
		}
		finally { }

		return new SearchQuery(value, stream) { IsFile = isFile, IsUrl = isUrl };
	}

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null)
	{
		if (IsUrl) {
			Upload = Value;
			Debug.WriteLine($"Skipping upload for {Value}");
		}
		else {
			engine ??= BaseUploadEngine.Default;
			var u = await engine.UploadFileAsync(Value);
			Upload = u;
		}

		return Upload;
	}

	#region IDisposable

	public void Dispose()
	{
		Stream?.Dispose();
	}

	#endregion
}