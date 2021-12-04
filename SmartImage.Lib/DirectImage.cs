using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace SmartImage.Lib
{
	
	public class DirectImage : IDisposable
	{
		public Uri Url { get; internal set; }

		public HttpResponseMessage Response { get; internal set; }

		public bool Equals(DirectImage other)
		{
			return Url == other?.Url && Equals(Response, other?.Response);
		}

		public override bool Equals(object obj)
		{
			return obj is DirectImage other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Response, Url);
		}

		public static bool operator ==(DirectImage left, DirectImage right)
		{
			return left is not null && left.Equals(right);
		}

		public static bool operator !=(DirectImage left, DirectImage right)
		{
			return left is not null && !left.Equals(right);
		}

		public void Dispose()
		{
			Response?.Dispose();
		}

		public override string ToString()
		{
			return $"{Url}";
		}
	}
}