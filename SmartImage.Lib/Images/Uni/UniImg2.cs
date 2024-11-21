// Author: Deci | Project: SmartImage.Test | Name: UniImg2.cs
// Date: 2024/11/20 @ 23:11:57

using System.IO.MemoryMappedFiles;
using CoenM.ImageHash;
using Flurl.Http;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SmartImage.Lib.Images.Uni;

#pragma warning disable CS0168, CS1998, CS0219, CS8602, CS8604
// TODO

public class UniImg2 : IDisposable
{

	public ulong? Hash { get; private set; }

	public MemoryMappedFile MappedFile { get; }

	public MemoryMappedViewStream View { get; private set; }

	public UniImageType Type { get; private set; }

	public Image<Rgba32> Image { get; private set; }

	[MNNW(true, nameof(Image))]
	public bool HasImage => Image != null;

	[MNNW(true, nameof(View))]
	public bool HasView => View != null;

	protected UniImg2(MemoryMappedFile mappedFile, UniImageType type)
	{
		MappedFile = mappedFile;
		Type       = type;
	}

	public async ValueTask<bool> AllocImage(CancellationToken ct = default)
	{
		if (!HasImage) {

			Image = await ISImage.LoadAsync<Rgba32>(View, ct);
		}

		return HasImage;
	}

	public bool AllocHash(CancellationToken ct = default)
	{
		/*if (HasImage) {
			Hash = ImageScanner.ImageHasher.Hash(Image);
		}
		else if (HasView) {
			Hash = ImageScanner.ImageHasher.Hash(View);
		}*/
		
		View.TrySeek();
		Hash = ImageScanner.ImageHasher.Hash(View);
		View.TrySeek();

		return Hash.HasValue;
	}

	public delegate ValueTask<IFlurlResponse> GetResponseAlloc(Url u);

	public static async Task<UniImg2> Alloc(Url u, [CBN] GetResponseAlloc req = null, bool allocInit = true,
	                                        CancellationToken ct = default)
	{
		req ??= ux =>
		{
			//
			return UniImageUri.GetResponseAsync(ux, ct);
		};

		UniImg2 ui2 = null;

		MemoryMappedFile       mappedFile;
		MemoryMappedViewStream v;

		try {

			if (Url.IsValid(u)) {
				using var res = await req(u);

				await using var responseStream = await res.GetStreamAsync();

				mappedFile = MemoryMappedFile.CreateNew(null, responseStream.Length);
				v          = mappedFile.CreateViewStream();

				responseStream.CopyTo(v);

			}
			else {
				throw new NotImplementedException();
			}

			ui2 = new UniImg2(mappedFile, UniImageType.Uri)
			{
				View = v
			};

			if (allocInit) {
				var allocImg  = await ui2.AllocImage(ct);
				var allocHash = ui2.AllocHash(ct);


			}
		}
		catch (Exception e) { }

		return ui2;
	}

	public static async Task<UniImg2> Alloc(string s, bool allocInit = true, CancellationToken ct = default)
	{
		UniImg2 ui2 = null;

		MemoryMappedFile mappedFile;

		try {
			if (File.Exists(s)) {
				mappedFile = MemoryMappedFile.CreateFromFile(s);

			}
			else {
				throw new NotImplementedException();
			}

			var v = mappedFile.CreateViewStream();

			ui2 = new UniImg2(mappedFile, UniImageType.File)
			{
				View = v
			};

			if (allocInit) {
				var allocImg  = await ui2.AllocImage(ct);
				var allocHash = ui2.AllocHash(ct);


			}
		}
		catch (Exception e) { }

		return ui2;
	}

	#region IDisposable

	public void Dispose()
	{
		Image?.Dispose();
		View?.Dispose();
		MappedFile?.Dispose();
	}

	#endregion

}