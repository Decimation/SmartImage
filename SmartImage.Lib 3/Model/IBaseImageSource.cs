// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

namespace SmartImage.Lib.Model;

public interface IBaseImageSource
{

	public bool HasImage { get; }

	public bool CanLoadImage { get; }

	public bool? IsThumbnail { get; }

	public int? Width { get; }

	public int? Height { get; }

	[CBN]
	public string DimensionString
	{
		get;
	}

	public bool LoadImage();

}