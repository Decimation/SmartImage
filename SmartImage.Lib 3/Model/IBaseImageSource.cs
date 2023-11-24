// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

using System.Drawing;

namespace SmartImage.Lib.Model;

public interface ISysImageSource : IBaseImageSource
{
	[MN]
	public Image Image { get; set; }

}

public interface IBaseImageSource
{

	public bool HasImage { get; }

	public bool CanLoadImage { get; }

	public bool? IsThumbnail { get; }

	public int? Width { get; }

	public int? Height { get; }

	public bool LoadImage([CBN] IImageLoader l = null);

}