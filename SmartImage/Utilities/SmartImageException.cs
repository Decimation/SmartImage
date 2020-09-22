using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

#nullable enable

namespace SmartImage.Utilities
{
	internal class SmartImageException : Exception
	{
		public SmartImageException() { }
		public SmartImageException([CanBeNull] string? message) : base(message) { }
	}
}
