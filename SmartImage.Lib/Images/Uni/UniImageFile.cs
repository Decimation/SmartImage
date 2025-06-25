// Author: Deci | Project: SmartImage.Lib | Name: UniImageFile.cs
// Date: 2024/07/17 @ 02:07:16

using Microsoft;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO.MemoryMappedFiles;

namespace SmartImage.Lib.Images.Uni;

public class UniImageFile : UniImage
{

	internal UniImageFile(object value, FileInfo fi)
		: base(value, UniImageType.File)
	{
		FileInfo = fi;
		FilePath = ValueString;
	}

	public FileInfo FileInfo { get; }

	public override string WriteToFile([CBN] string fn = null, [CBN] Action<IImageProcessingContext> operation = null)
	{
		if (!HasFile) {
			throw new FileNotFoundException(ValueString);
		}

		return ValueString;
	}

#region Overrides of UniImage

	public override async Task<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (!HasImage) {
			try {
				var fullName = FileInfo.FullName;

				using var stream     = File.OpenRead(fullName);
				Size = stream.Length;

				Image = await ISImage.LoadAsync<Rgba32>(stream, ct);
				
			}
			catch (Exception exception) {
				return false;
			}

		}

		return HasImage;
	}

#endregion


	public static bool IsFileType(object o, out FileInfo f)
	{
		f = null;

		if (o is string { } s && File.Exists(s)) {
			f = new FileInfo(s);
		}

		return f != null;
	}

}