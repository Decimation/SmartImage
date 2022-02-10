using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Kantan.Net;

namespace SmartImage.Lib.Utilities;

public readonly struct MediaResourceInfo : IDisposable
{
	[MN]
	public MediaResource Resource { get; internal init; }

	[MN]
	public HttpResponseMessage Message { get; internal init; }

	/// <summary>
	/// Whether this is a binary URI
	/// </summary>
	public bool IsUri { get; internal init; }

	/// <summary>
	/// Whether this is a file
	/// </summary>
	public bool IsFile { get; internal init; }

	/// <summary>
	/// <see cref="IsUri"/> <c>||</c> <see cref="IsFile"/>
	/// </summary>
	public bool IsValid => IsUri || IsFile;


	/// <returns><see cref="IsValid"/></returns>
	public static explicit operator bool(MediaResourceInfo mri) => mri.IsValid;

	public override string ToString()
	{
		return $"{nameof(Resource)}: {Resource}, " +
		       $"{nameof(Message)}: {Message}, " +
		       $"{nameof(IsUri)}: {IsUri}, " +
		       $"{nameof(IsFile)}: {IsFile}" +
		       $"{nameof(IsValid)}: {IsValid}";
	}

	public void Dispose()
	{
		Resource?.Dispose();
		Message?.Dispose();
	}
}