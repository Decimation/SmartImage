using System;
using System.Threading.Tasks;
using Kantan.Numeric;
using Novus.OS;

namespace SmartImage.Lib.Engines.Upload.Base;

public abstract class BaseUploadEngine
{
	/// <summary>
	/// Max file size, in MB
	/// </summary>
	public abstract int MaxSize { get; }

	public abstract string Name { get; }

	protected string EndpointUrl { get; }

	protected BaseUploadEngine(string s)
	{
		EndpointUrl = s;
	}

	public abstract Task<Uri> UploadFileAsync(string file);

	protected bool IsFileSizeValid(string file)
	{
		double fileSizeMegabytes =
			MathHelper.ConvertToUnit(FileSystem.GetFileSize(file), MetricPrefix.Mega);

		var b = fileSizeMegabytes >= MaxSize;

		return !b;
	}

	protected void Verify(string file)
	{
		if (String.IsNullOrWhiteSpace(file)) {
			throw new ArgumentNullException(nameof(file));
		}

		if (!IsFileSizeValid(file)) {
			throw new ArgumentException($"File {file} is too large (max {MaxSize} MB) for {Name}");
		}
	}
}