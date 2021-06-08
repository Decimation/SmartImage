using System;
using Novus.Win32;
using SimpleCore.Numeric;

namespace SmartImage.Lib.Upload
{
	public abstract class BaseUploadEngine
	{
		/// <summary>
		/// Max file size, in MB
		/// </summary>
		public abstract int MaxSize { get; }

		public abstract string Name { get; }


		public abstract Uri Upload(string file);


		protected bool IsFileSizeValid(string file)
		{
			double fileSizeMegabytes =
				MathHelper.ConvertToUnit(FileSystem.GetFileSize(file), MetricUnit.Mega);

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
}