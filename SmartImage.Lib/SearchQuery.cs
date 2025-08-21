// ReSharper disable RedundantUsingDirective.Global

#region Global usings

global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using NNW = System.Diagnostics.CodeAnalysis.NotNullWhenAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
global using NN2 = JetBrains.Annotations.NotNullAttribute;
global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
global using CMN = System.Runtime.CompilerServices.CallerMemberNameAttribute;
global using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;
global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
global using Url = Flurl.Url;
global using USI = JetBrains.Annotations.UsedImplicitlyAttribute;
global using CAE = System.Runtime.CompilerServices.CallerArgumentExpressionAttribute;
global using ISImage = SixLabors.ImageSharp.Image;
global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using INN = JetBrains.Annotations.ItemNotNullAttribute;
global using MURV = JetBrains.Annotations.MustUseReturnValueAttribute;
global using R1 = SmartImage.Lib.Resources;
global using CA = JetBrains.Annotations.ContractAnnotationAttribute;

#endregion

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
using SmartImage.Lib.Utilities;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Images.Uni;

[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE)]
[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_UI)]
[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_RDX)]
[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_LIB_UNITTEST)]

namespace SmartImage.Lib;

public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>
{

#region Project names

	internal const string PROJ_SMARTIMAGE              = "SmartImage";
	internal const string PROJ_SMARTIMAGE_TEST         = $"{PROJ_SMARTIMAGE}.Test";
	internal const string PROJ_SMARTIMAGE_UI           = $"{PROJ_SMARTIMAGE}.UI";
	internal const string PROJ_SMARTIMAGE_UI2          = $"{PROJ_SMARTIMAGE_UI}2";
	internal const string PROJ_SMARTIMAGE_RDX          = $"{PROJ_SMARTIMAGE}.Rdx";
	internal const string PROJ_SMARTIMAGE_LIB          = $"{PROJ_SMARTIMAGE}.Lib";
	internal const string PROJ_SMARTIMAGE_LIB_UNITTEST = $"{PROJ_SMARTIMAGE_LIB}.UnitTest";
	

#endregion


	[MN]
	public Url Upload { get; private set; }

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Url.IsValid(Upload);

	public UniImage Source { get; }

	internal SearchQuery(UniImage img, Url upload)
	{
		Source = img;
		Upload = upload;

		// Size = Uni == null ? default : Uni.Stream.Length;
	}

	internal SearchQuery(UniImage img) : this(img, null) { }

	static SearchQuery() { }

	public static readonly SearchQuery Null = new(UniImage.Null);

	public static async Task<SearchQuery> TryCreateAsync(object o, CancellationToken t = default)
	{
		var task = await UniImage.TryCreateAsync(o, ct: t);

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


		if (Source.IsUri) {
			Upload = Source.Value;

			// Size   = BaseSearchEngine.NA_SIZE;
			// var fmt = await ISImage.DetectFormatAsync(Stream);

			Debug.WriteLine($"Skipping upload for {Source.Value}", nameof(UploadAsync));
		}
		else {
			// fu = await test(fu);

			string fu;

			if (Source.IsFile) {
				fu = Source.Value;
			}
			else {
				// fu = Source.WriteToFile();
				fu = null;

				if (Source.TryWriteToFile()) {
					fu = Source.LocalFilePath;
				}

				Trace.WriteLine($"Wrote to file {fu}");
			}

			engine ??= BaseUploadEngine.Default;

			UploadResult u = await engine.UploadFileAsync(fu, ct);
			Url          url;

			if (!u.IsValid.GetValueOrDefault()) {
				url = null;
				Debug.WriteLine($"{u} is invalid!");

			}
			else {
				url = u.Url;

			}

			// TODO: AUTO-RETRY

			Upload = url;

			u.Dispose();
		}

		return Upload;
	}

	public void Dispose()
	{
		Trace.WriteLine($"Disposing {Source}");
		Source?.Dispose();
	}

	public override string ToString()
	{
		return $"{Source}: {IsUploaded}";
	}

#region Equality members

	public bool Equals(SearchQuery other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

		return Equals(Source, other.Source) && Equals(Upload, other.Upload);
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is SearchQuery other && Equals(other));
	}

	public override int GetHashCode()
	{
		// return HashCode.Combine(Uni, Upload, Size);
		return HashCode.Combine(Source);

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