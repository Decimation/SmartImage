using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
#nullable enable
namespace SmartImage.Lib.Utilities
{
	public class SmartImageException : Exception
	{
		public SmartImageException() { }
		public SmartImageException([CanBeNull] string? message) : base(message) { }
	}
}
