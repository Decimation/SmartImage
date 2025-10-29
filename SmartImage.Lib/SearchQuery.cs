// ReSharper disable RedundantUsingDirective.Global



#region Global usings

global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using NNW = System.Diagnostics.CodeAnalysis.NotNullWhenAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
global using NN2 = JetBrains.Annotations.NotNullAttribute;
global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
global using MNN = System.Diagnostics.CodeAnalysis.MemberNotNullAttribute;
global using CMN = System.Runtime.CompilerServices.CallerMemberNameAttribute;
global using JPN = System.Text.Json.Serialization.JsonPropertyNameAttribute;
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
using Flurl;
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
using System.ComponentModel;
using SmartImage.Lib.Engines.Results;

#region 

[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE)]
[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_UI)]
[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_RDX)]
[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_LIB_UNITTEST)]

#endregion

namespace SmartImage.Lib;

public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>, INotifyPropertyChanged
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


	// [MN]
	// public Url Upload { get; private set; }

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Upload != null && Url.IsValid(Upload.Url);

	private UploadResult m_upload;

	[MN]
	public UploadResult Upload
	{
		get => m_upload;
		private set
		{
			if (SetField(ref m_upload, value)) {
				OnPropertyChanged(nameof(IsUploaded));
			}
		}
	}

	public UniImage Source { get; }

	internal SearchQuery(UniImage img, UploadResult upload)
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
		var ui = await UniImage.TryCreateAsync(o, ct: t);

		if (ui != UniImage.Null) {
			return new SearchQuery(ui);

		}
		else {
			return Null;
		}
	}

	public async ValueTask<bool> TryUploadAsync(BaseUploadEngine uploadEngine = null, CancellationToken ct = default)
	{
		//todo
		if (IsUploaded) {
			return true;
		}

		uploadEngine    ??= BaseUploadEngine.Default;
		Upload =   await uploadEngine.UploadAsync(Source, ct);
		return IsUploaded;
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

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CMN] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CMN] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

}