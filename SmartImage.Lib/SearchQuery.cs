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
using System.Runtime.CompilerServices;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Images.Uni;
using System.ComponentModel;
using SmartImage.Shared;

#region 

[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_UI)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_RDX)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_LIB_UNITTEST)]

#endregion

namespace SmartImage.Lib;

public sealed class SearchQuery : IDisposable, IEquatable<SearchQuery>, INotifyPropertyChanged
{

	// [MN]
	// public Url Upload { get; private set; }

	[MNNW(true, nameof(Upload))]
	public bool IsUploaded => Upload != null && Url.IsValid(Upload.Url);

	[MN]
	public UploadResult Upload
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

	internal SearchQuery(UniImage img, UploadResult upload)
	{
		Source = img;
		Upload = upload;

		// Length = Uni == null ? default : Uni.Stream.Length;
	}

	internal SearchQuery(UniImage img) : this(img, null) { }

	static SearchQuery() { }

	public static readonly SearchQuery Null = new(null);

	public static async Task<SearchQuery> TryCreateAsync(object o, CancellationToken t = default)
	{
		var ui = await UniImage.TryCreateAsync(o, ct: t);

		if (ui != null) {
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
		// return HashCode.Combine(Uni, Upload, Length);
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