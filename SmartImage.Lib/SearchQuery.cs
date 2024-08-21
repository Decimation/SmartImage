global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
global using ISImage = SixLabors.ImageSharp.Image;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Microsoft;
using Novus.FileTypes.Uni;
using Novus.Streams;
using Novus.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Images.Uni;

[assembly: InternalsVisibleTo("SmartImage")]
[assembly: InternalsVisibleTo("SmartImage.UI")]
[assembly: InternalsVisibleTo("SmartImage.Rdx")]
[assembly: InternalsVisibleTo("Test")]

namespace SmartImage.Lib;

public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>
{

	[MN]
	public Url Upload { get; internal set; }

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Url.IsValid(Upload);

	public UniImage Image { get; }

	internal SearchQuery(UniImage img, Url upload)
	{
		Image  = img;
		Upload = upload;

		// Size = Uni == null ? default : Uni.Stream.Length;
	}

	internal SearchQuery(UniImage img) : this(img, null) { }

	static SearchQuery() { }

	public static readonly SearchQuery Null = new(UniImage.Null);

	public static async Task<SearchQuery> TryCreateAsync(object o, CancellationToken t = default)
	{
		var task = await UniImage.TryCreateAsync(o, t: t);

		if (task != UniImage.Null) {
			return new SearchQuery(task);

		}
		else {
			return Null;
		}
	}

	public async Task<Url> UploadAsync(BaseUploadEngine engine = null, CancellationToken ct = default)
	{
		if (IsUploaded) {
			return Upload;
		}

		string fu = Image.ValueString;

		if (Image.IsUri) {
			Upload = fu;

			// Size   = BaseSearchEngine.NA_SIZE;
			// var fmt = await ISImage.DetectFormatAsync(Stream);

			Debug.WriteLine($"Skipping upload for {Image.ValueString}", nameof(UploadAsync));
		}
		else {
			// fu = await test(fu);

			engine ??= BaseUploadEngine.Default;

			UploadResult u = await engine.UploadFileAsync(fu, ct);
			Url          url;

			if (!u.IsValid) {
				url = null;
				Debug.WriteLine($"{u} is invalid!");

				// Debugger.Break();
			}
			else {
				url = u.Url;

			}

			// TODO: AUTO-RETRY
			/*
			UploadResult u = await UploadAutoAsync(engine, fu, ct);
			Url          url = u?.Url;
			*/

			/*if (!u.IsValid) {
				engine = BaseUploadEngine.All[Array.IndexOf(BaseUploadEngine.All, engine) + 1];
				Debug.WriteLine($"{u.Response.ResponseMessage} failed, retrying with {engine.Name}");
				u = await engine.UploadFileAsync(Uni.Value.ToString(), ct);
			}*/

			Upload = url;

			/*if (u.Response is { }) {
				Size = NetHelper.GetContentLength(u.Response) ?? Size;
			}*/
			// Size = u.Size ?? Size;
			u.Dispose();
		}

		return Upload;
	}

	public void Dispose()
	{
		Image?.Dispose();
	}

	public override string ToString()
	{
		return $"{Image}: {IsUploaded}";
	}

	#region Equality members

	public bool Equals(SearchQuery other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Equals(Image, other.Image) && Equals(Upload, other.Upload);
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is SearchQuery other && Equals(other));
	}

	public override int GetHashCode()
	{
		// return HashCode.Combine(Uni, Upload, Size);
		return HashCode.Combine(Image);

		// return Uni.GetHashCode();
	}

	public static bool operator ==(SearchQuery left, SearchQuery right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(SearchQuery left, SearchQuery right)
	{
		return !Equals(left, right);
	}

	#endregion

}
