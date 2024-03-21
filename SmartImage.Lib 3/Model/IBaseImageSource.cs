// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

namespace SmartImage.Lib.Model;

[Flags]
public enum ResultItemProperties
{

	None      = 0,
	Thumbnail = 1 << 0,
	CanDownload=1<<1,

}

public static class Ext
{

	public static ResultItemProperties Add(this ResultItemProperties r, ResultItemProperties x)
	{
		return r | x;
	}

	public static bool Has(this ResultItemProperties r, ResultItemProperties x)
	{
		return (r & x) != 0;
	}

	public static ResultItemProperties Rm(this ResultItemProperties r, ResultItemProperties x)
	{
		return r & ~x;
	}

}

public interface IBaseImageSource
{

	public ResultItemProperties Properties { get; set; }

	public bool HasImage { get; }

	public bool CanLoadImage { get; }

	public bool IsThumbnail { get; }

	public int? Width { get; }

	public int? Height { get; }

	[CBN]
	public string DimensionString { get; }

	public bool LoadImage();

}