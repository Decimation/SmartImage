// Author: Deci | Project: SmartImage.Lib | Name: AllocImageFile.cs
// Date: 2024/07/17 @ 02:07:16

namespace SmartImage.Lib.Images.Alloc;

public class AllocImageFile : AllocImage
{

	internal AllocImageFile(FileInfo fi) : base(fi.FullName, UniImageType.File)
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