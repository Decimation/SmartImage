// Author: Deci | Project: SmartImage.Lib | Name: UniImageFile.cs
// Date: 2024/07/17 @ 02:07:16

using Microsoft;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.MemoryMappedFiles;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace SmartImage.Lib.Images.Uni;

public class UniImageFile : UniImage
{

	internal UniImageFile(FileInfo fi) : base(fi.FullName, UniImageType.File)
	{
		LocalFileInfo = fi;
		LocalFilePath = Value;
	}

	public FileInfo LocalFileInfo { get; }

	public override string WriteImageToFile(string fn = null)
	{
		if (!HasFilePath) {
			throw new FileNotFoundException(Value);
		}

		return Value;
	}

	protected override async Task<bool> AllocAsync(CancellationToken ct = default)
	{
		if (HasBytes) {
			goto ret;
		}

		Bytes = await File.ReadAllBytesAsync(Value, ct);

	ret:
		return HasBytes;
	}

	public static bool IsFileType(object o, out FileInfo f)
	{
		f = null;

		if (o is string { } s && File.Exists(s)) {
			f = new FileInfo(s);
		}

		return f != null && ImageScanner.FormatExtensions.Contains(f.Extension[1..]);
	}

}