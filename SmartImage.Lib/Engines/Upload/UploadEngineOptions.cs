// Author: Deci | Project: SmartImage.Lib | Name: UploadEngineOptions.cs
// Date: 2024/12/06 @ 03:12:53

namespace SmartImage.Lib.Engines.Upload;

public enum UploadEngineOptions
{

	None = 0,

	/// <summary>
	/// <see cref="CatboxEngine"/>
	/// </summary>
	Catbox,

	/// <summary>
	/// <see cref="LitterboxEngine"/>
	/// </summary>
	Litterbox,

	/// <summary>
	/// <see cref="PomfEngine"/>
	/// </summary>
	Pomf,

}