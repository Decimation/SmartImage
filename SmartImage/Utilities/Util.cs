using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleCore.Utilities;

namespace SmartImage.Utilities
{
	internal static class Util
	{
		internal static StringBuilder AppendColor(this StringBuilder sb, Color c, object value)
		{
			return sb.Append(value.ToString()!.AddColor(c));
		}

		internal static StringBuilder AppendLabelWithColor(this StringBuilder sb,
			Color ck, string k, Color cv, object v)
		{
			return sb.AppendColor(ck, k + ": ").AppendColor(cv, v);
		}
	}
}
