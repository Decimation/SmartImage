// Author: Deci | Project: SmartImage.Lib | Name: UniImageFile.cs
// Date: 2024/07/17 @ 02:07:16

using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images.Uni;

public class UniImageFile : UniImage
{

	internal UniImageFile(FileInfo fi) : base(fi.FullName, UniImageType.File)
	{
		LocalFileInfo = fi;
		LocalFilePath = Value;
	}

	public FileInfo LocalFileInfo { get; }

	public override string Name => LocalFileInfo.Name;

	public override async Task<bool> AllocSourceAsync(CancellationToken ct = default)
	{
		if (HasBytes) {
			goto ret;
		}

		Bytes = await File.ReadAllBytesAsync(Value, ct);

	ret:
		return HasBytes;
	}

	public override string WriteImageToFile(string fn = null)
	{
		if (!HasLocalFilePath) {
			throw new FileNotFoundException(Value);
		}

		return Value;
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