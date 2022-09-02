using Novus.OS;
using Novus.Utilities;

namespace SmartImage.Lib.Engines;

public abstract class BaseUploadEngine
{
	/// <summary>
	/// Max file size, in bytes
	/// </summary>
	public abstract int MaxSize { get; }

	public abstract string Name { get; }

	protected string EndpointUrl { get; }

	protected BaseUploadEngine(string s)
	{
		EndpointUrl = s;
	}

	public static BaseUploadEngine Default { get; } = new LitterboxEngine();

	public abstract Task<Url> UploadFileAsync(string file);

	private protected bool IsFileSizeValid(string file)
	{
		var bytes = FileSystem.GetFileSize(file);

		var b = bytes >= MaxSize;

		return !b;
	}

	protected void Verify(string file)
	{
		if (string.IsNullOrWhiteSpace(file)) {
			throw new ArgumentNullException(nameof(file));
		}

		if (!IsFileSizeValid(file)) {
			throw new ArgumentException($"File {file} is too large (max {MaxSize} MB) for {Name}");
		}
	}

	public static readonly BaseUploadEngine[] All = ReflectionHelper.CreateAllInAssembly<BaseUploadEngine>(TypeProperties.Subclass).ToArray();
}