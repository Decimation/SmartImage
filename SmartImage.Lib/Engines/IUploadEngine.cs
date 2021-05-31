using System;
using Novus.Win32;
using SimpleCore.Numeric;

namespace SmartImage.Lib.Engines
{
	public interface IUploadEngine
	{
		public string Name { get; }

		/// <summary>
		/// Max file size, in MB
		/// </summary>
		public int MaxSize { get; }

		public Uri Upload(string file);


		protected static void Verify(IUploadEngine e, string file)
		{
			//todo

			if (String.IsNullOrWhiteSpace(file)) {
				throw new ArgumentNullException(nameof(file));
			}

			if (!e.IsFileSizeValid(file)) {
				throw new ArgumentException($"File {file} is too large (max {e.MaxSize} MB) for {e.Name}");
			}
		}

		protected bool IsFileSizeValid(string file)
		{
			double fileSizeMegabytes =
				MathHelper.ConvertToUnit(FileSystem.GetFileSize(file), MetricUnit.Mega);

			var b = fileSizeMegabytes >= MaxSize;

			return !b;


		}
	}
}