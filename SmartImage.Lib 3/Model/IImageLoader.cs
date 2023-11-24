using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartImage.Lib.Model
{
	public interface IImageLoader
	{
		bool LoadImage(IBaseImageSource p);

	}
}
