using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace SmartImage.Lib
{
	
	public class DirectImage : IDisposable
	{
		public Uri Url { get; internal set; }

		public Stream Stream { get; internal set; }

		public IRestResponse Response { get; internal set; }

		public bool Equals(DirectImage other)
		{
			
			return Url == other?.Url && Equals(Stream, other?.Stream) 
			                        && Equals(Response, other?.Response);
		}

		public override bool Equals(object obj)
		{
			return obj is DirectImage other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Stream, Response, Url);
		}

		public static bool operator ==(DirectImage left, DirectImage right)
		{
			return !ReferenceEquals(left, null) && left.Equals(right);

		}

		public static bool operator !=(DirectImage left, DirectImage right)
		{
			return !ReferenceEquals(left, null) && !left.Equals(right);

		}

		public void Dispose()
		{
			Stream?.Dispose();
			// Response          = null;

			if (Response is {}) {
				Response.RawBytes = null;
				Response.Content  = null;

			}
		}

		public override string ToString()
		{
			return $"{Url}";
		}
	}
}