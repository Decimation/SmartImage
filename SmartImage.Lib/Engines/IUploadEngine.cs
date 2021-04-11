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

		public bool FileSizeValid(string file)
		{
			double fileSizeMegabytes =
				MathHelper.ConvertToUnit(FileSystem.GetFileSize(file), MetricUnit.Mega);

			var b = fileSizeMegabytes >= MaxSize;

			return !b;


		}
	}
}