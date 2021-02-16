using JetBrains.Annotations;
using System;

#nullable enable

namespace SmartImage.Utilities
{
	internal class SmartImageException : Exception
	{
		public SmartImageException() { }
		public SmartImageException([CanBeNull] string? message) : base(message) { }
	}
}