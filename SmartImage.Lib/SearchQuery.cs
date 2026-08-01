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
using System.ComponentModel;
using Flurl;
using Microsoft.Extensions.Logging;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Utilities;
using SmartImage.Shared;

#region

[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_UI)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_RDX)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_LIB_UNITTEST)]

#endregion

namespace SmartImage.Lib;

// TODO: This should be a AllocImage?
public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>, INotifyPropertyChanged, IUploadable, IAllocImageView<AllocImage>
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

	public AllocImage AllocImage { get; }

	private SearchQuery(AllocImage img, UploadResult upload)
	{
		AllocImage = img;
		Upload = upload;

		// Length = Uni == null ? default : Uni.Stream.Length;
	}

	private SearchQuery(AllocImage img) : this(img, null) { }

	static SearchQuery() { }

	public static readonly SearchQuery Null = new(null);

	public static async Task<SearchQuery> TryCreateAsync(object o, CancellationToken t = default)
	{
		var ui = (AllocImage) await AllocImage.FromSourceAsync(o, ct: t);

		return ui != null ? new SearchQuery(ui) : Null;

	}

	public async ValueTask<bool> TryUploadAsync(IUploadEngine ue = null, CancellationToken ct = default)
	{
		if (IsUploaded) {
			return true;
		}

		ue ??= BaseUploadEngine.GetUploadEngine(SearchConfig.UE_DEFAULT); //todo

		ue.Verify(AllocImage);

		if (AllocImage is AllocImageUrl { } uri) {
			s_logger.LogTrace("Not uploading {Uni} {Val}", AllocImage, AllocImage.Value);
			Upload = new UploadResult(uri.Url, uri.Length) { };
		}
		else {
			Upload = await ue.UploadFileAsync(AllocImage.Value, ct);
		}

		return IsUploaded;
	}

	public void Dispose()
	{
		s_logger.LogTrace($"Disposing {AllocImage}");
		AllocImage?.Dispose();
	}

	public override string ToString()
	{
		return $"{AllocImage}: {IsUploaded}";
	}

#region Equality members

	public bool Equals(SearchQuery other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

		return Equals(AllocImage, other.AllocImage) && Equals(Upload, other.Upload);
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is SearchQuery other && Equals(other));
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(AllocImage);
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