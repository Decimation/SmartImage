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
global using MDR = JetBrains.Annotations.MustDisposeResourceAttribute;
global using R1 = SmartImage.Lib.Resources;
global using CA = JetBrains.Annotations.ContractAnnotationAttribute;

#endregion

using System.Diagnostics;
using System.Runtime.CompilerServices;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Images.Uni;
using System.ComponentModel;
using Flurl;
using Microsoft.Extensions.Logging;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Utilities;
using SmartImage.Shared;

#region

[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_UI)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_RDX)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_LIB_UNITTEST)]

#endregion

namespace SmartImage.Lib;

// TODO: This should be a UniImage?
public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>, INotifyPropertyChanged, IUploadable
{

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchQuery));

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Upload != null && Url.IsValid(Upload.Url);

	[MN]
	public IUploadResult Upload
	{
		get;
		private set
		{
			if (SetField(ref field, value)) {
				OnPropertyChanged(nameof(IsUploaded));
			}
		}
	}

	public UniImage Source { get; }

	private SearchQuery(UniImage img, UploadResult upload)
	{
		Source = img;
		Upload = upload;

		// Length = Uni == null ? default : Uni.Stream.Length;
	}

	private SearchQuery(UniImage img) : this(img, null) { }

	static SearchQuery() { }

	public static readonly SearchQuery Null = new(null);

	public static async Task<SearchQuery> TryCreateAsync(object o, CancellationToken t = default)
	{
		var ui = await UniImage.FromSourceAsync(o, ct: t);

		return ui != null ? new SearchQuery(ui) : Null;

	}

	public async ValueTask<bool> TryUploadAsync(IUploadEngine ue = null, CancellationToken ct = default)
	{
		if (IsUploaded) {
			return true;
		}

		ue ??= BaseUploadEngine.GetUploadEngine(SearchConfig.UE_DEFAULT); //todo

		ue.Verify(Source);

		if (Source is UniImageUrl { } uri) {
			s_logger.LogTrace("Not uploading {Uni} {Val}", Source, Source.Value);
			Upload = new UploadResult(uri.Url, uri.Length) { };
		}
		else {
			Upload = await ue.UploadFileAsync(Source.Value, ct);
		}

		return IsUploaded;
	}

	public void Dispose()
	{
		s_logger.LogTrace($"Disposing {Source}");
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
		return HashCode.Combine(Source);
	}

	public static bool operator ==(SearchQuery left, SearchQuery right)
		=> Equals(left, right);

	public static bool operator !=(SearchQuery left, SearchQuery right)
		=> !Equals(left, right);

#endregion

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CMN] string propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	private bool SetField<T>(ref T field, T value, [CMN] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

}